using Microsoft.EntityFrameworkCore;
using SafeNavigation.Application.Abstractions;
using SafeNavigation.Application.Errors;
using SafeNavigation.Application.Models;
using SafeNavigation.Domain.Entities;

namespace SafeNavigation.Application.Services;

public sealed class RulesService(ISafeNavigationDbContext db, IClock clock)
{
    public async Task<IReadOnlyList<BlockingRuleDto>> ListAsync(Guid guardianId, CancellationToken cancellationToken)
    {
        return await db.BlockingRules
            .Where(x => x.GuardianId == guardianId)
            .OrderBy(x => x.Priority)
            .ThenBy(x => x.Value)
            .Select(x => ToDto(x))
            .ToListAsync(cancellationToken);
    }

    public async Task<BlockingRuleDto> CreateAsync(Guid guardianId, CreateRuleRequest request, CancellationToken cancellationToken)
    {
        await EnsureScopeAsync(guardianId, request.ChildId, request.DeviceId, cancellationToken);

        var rule = new BlockingRule
        {
            GuardianId = guardianId,
            ChildId = request.ChildId,
            DeviceId = request.DeviceId,
            RuleType = request.RuleType,
            Value = NormalizeValue(request.Value),
            Action = request.Action,
            Priority = request.RuleType == "allow_domain" ? 1 : 100,
            Enabled = true,
            CreatedAt = clock.UtcNow,
            UpdatedAt = clock.UtcNow
        };

        db.BlockingRules.Add(rule);
        AddAudit(guardianId, "rule.created", rule.Id);
        await db.SaveChangesAsync(cancellationToken);
        return ToDto(rule);
    }

    public async Task<BlockingRuleDto> UpdateAsync(
        Guid guardianId,
        Guid ruleId,
        UpdateRuleRequest request,
        CancellationToken cancellationToken)
    {
        var rule = await db.BlockingRules.FirstOrDefaultAsync(x => x.Id == ruleId && x.GuardianId == guardianId, cancellationToken);
        if (rule is null) throw new ResourceNotFoundException("Rule not found.");

        await EnsureScopeAsync(guardianId, request.ChildId, request.DeviceId, cancellationToken);

        rule.RuleType = request.RuleType;
        rule.Value = NormalizeValue(request.Value);
        rule.Action = request.Action;
        rule.Priority = request.Priority;
        rule.Enabled = request.Enabled;
        rule.ChildId = request.ChildId;
        rule.DeviceId = request.DeviceId;
        rule.UpdatedAt = clock.UtcNow;

        AddAudit(guardianId, "rule.updated", rule.Id);
        await db.SaveChangesAsync(cancellationToken);
        return ToDto(rule);
    }

    public async Task DeleteAsync(Guid guardianId, Guid ruleId, CancellationToken cancellationToken)
    {
        var rule = await db.BlockingRules.FirstOrDefaultAsync(x => x.Id == ruleId && x.GuardianId == guardianId, cancellationToken);
        if (rule is null) return;

        db.BlockingRules.Remove(rule);
        AddAudit(guardianId, "rule.deleted", rule.Id);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<UnblockRequestDto>> ListUnblockRequestsAsync(Guid guardianId, CancellationToken cancellationToken)
    {
        return await db.UnblockRequests
            .Where(x => x.Device!.Child!.GuardianId == guardianId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => ToDto(x))
            .ToListAsync(cancellationToken);
    }

    public async Task<UnblockRequestDto> CreateUnblockRequestAsync(
        Guid deviceId,
        CreateUnblockRequest request,
        CancellationToken cancellationToken)
    {
        var deviceExists = await db.Devices.AnyAsync(x => x.Id == deviceId && x.Status == "active", cancellationToken);
        if (!deviceExists) throw new ResourceNotFoundException("Device not found.");

        var unblockRequest = new UnblockRequest
        {
            DeviceId = deviceId,
            Domain = NormalizeValue(request.Domain),
            Message = string.IsNullOrWhiteSpace(request.Message) ? null : request.Message.Trim(),
            CreatedAt = clock.UtcNow
        };

        db.UnblockRequests.Add(unblockRequest);
        db.AuditLogs.Add(new AuditLog
        {
            ActorType = "device",
            ActorId = deviceId,
            Action = "unblock_request.created",
            EntityType = "unblock_request",
            EntityId = unblockRequest.Id,
            CreatedAt = clock.UtcNow
        });

        await db.SaveChangesAsync(cancellationToken);
        return ToDto(unblockRequest);
    }

    public async Task<UnblockRequestDto> DecideUnblockRequestAsync(
        Guid guardianId,
        Guid requestId,
        UnblockDecisionRequest request,
        CancellationToken cancellationToken)
    {
        var unblockRequest = await db.UnblockRequests
            .Include(x => x.Device)
            .ThenInclude(x => x!.Child)
            .FirstOrDefaultAsync(x => x.Id == requestId && x.Device!.Child!.GuardianId == guardianId, cancellationToken);

        if (unblockRequest is null) throw new ResourceNotFoundException("Unblock request not found.");

        unblockRequest.Status = request.Decision;
        unblockRequest.DecisionReason = request.Reason?.Trim();
        unblockRequest.DecidedAt = clock.UtcNow;

        AddAudit(guardianId, $"unblock_request.{request.Decision}", unblockRequest.Id);
        await db.SaveChangesAsync(cancellationToken);
        return ToDto(unblockRequest);
    }

    private async Task EnsureScopeAsync(Guid guardianId, Guid? childId, Guid? deviceId, CancellationToken cancellationToken)
    {
        if (childId is not null)
        {
            var ownsChild = await db.Children.AnyAsync(x => x.Id == childId && x.GuardianId == guardianId, cancellationToken);
            if (!ownsChild) throw new ForbiddenOperationException("Child does not belong to guardian.");
        }

        if (deviceId is null) return;

        var ownsDevice = await db.Devices.AnyAsync(x => x.Id == deviceId && x.Child!.GuardianId == guardianId, cancellationToken);
        if (!ownsDevice) throw new ForbiddenOperationException("Device does not belong to guardian.");
    }

    private void AddAudit(Guid guardianId, string action, Guid entityId)
    {
        db.AuditLogs.Add(new AuditLog
        {
            ActorType = "guardian",
            ActorId = guardianId,
            Action = action,
            EntityType = "blocking_rule",
            EntityId = entityId,
            CreatedAt = clock.UtcNow
        });
    }

    private static string NormalizeValue(string value) => value.Trim().TrimEnd('.').ToLowerInvariant();

    private static BlockingRuleDto ToDto(BlockingRule rule) =>
        new(rule.Id, rule.RuleType, rule.Value, rule.Action, rule.Priority, rule.Enabled, rule.ChildId, rule.DeviceId);

    private static UnblockRequestDto ToDto(UnblockRequest request) =>
        new(request.Id, request.DeviceId, request.Domain, request.Message, request.Status, request.DecisionReason, request.CreatedAt, request.DecidedAt);
}
