using Microsoft.EntityFrameworkCore;
using SafeNavigation.Application.Abstractions;
using SafeNavigation.Application.Errors;
using SafeNavigation.Application.Models;
using SafeNavigation.Domain.Entities;

namespace SafeNavigation.Application.Services;

public sealed class SyncService(ISafeNavigationDbContext db, IClock clock, SyncAlertFactory alertFactory)
{
    public async Task<SyncBatchResponse> IngestAsync(
        Guid deviceActorId,
        SyncBatchRequest request,
        CancellationToken cancellationToken)
    {
        if (deviceActorId != request.DeviceId) throw new ForbiddenOperationException();

        var device = await db.Devices
            .Include(x => x.Config)
            .Include(x => x.Child)
            .FirstOrDefaultAsync(x => x.Id == request.DeviceId, cancellationToken);
        if (device is null) throw new ResourceNotFoundException("Device not found.");

        var existingBatch = await db.SyncBatches.FirstOrDefaultAsync(
            x => x.DeviceId == request.DeviceId && x.ClientBatchId == request.ClientBatchId,
            cancellationToken);
        if (existingBatch is not null) return ToResponse(existingBatch, device);

        var accepted = 0;
        accepted += await UpsertAppUsagesAsync(device.Id, request.AppUsages, cancellationToken);
        accepted += await UpsertDomainAccessesAsync(device, request.DomainAccesses, cancellationToken);
        accepted += await AddNewBlockAttemptsAsync(device, request.BlockAttempts, cancellationToken);

        var batch = new SyncBatch
        {
            DeviceId = request.DeviceId,
            ClientBatchId = request.ClientBatchId,
            StartedAt = clock.UtcNow,
            CompletedAt = clock.UtcNow,
            Status = "accepted",
            RecordsReceived = accepted,
            RecordsAccepted = accepted
        };
        device.LastSyncAt = clock.UtcNow;
        db.SyncBatches.Add(batch);

        await db.SaveChangesAsync(cancellationToken);
        return ToResponse(batch, device);
    }

    private async Task<int> UpsertAppUsagesAsync(
        Guid deviceId,
        IReadOnlyList<AppUsageRecord>? records,
        CancellationToken cancellationToken)
    {
        if (records is null || records.Count == 0) return 0;

        var clientIds = records.Select(x => x.LocalId).ToList();
        var existing = await db.AppUsages
            .Where(x => x.DeviceId == deviceId && clientIds.Contains(x.ClientRecordId))
            .ToDictionaryAsync(x => x.ClientRecordId, cancellationToken);

        foreach (var record in records)
        {
            if (existing.TryGetValue(record.LocalId, out var usage))
            {
                SyncRecordMerger.Merge(usage, record);
                continue;
            }

            db.AppUsages.Add(new AppUsage
            {
                DeviceId = deviceId,
                ClientRecordId = record.LocalId,
                PackageName = record.PackageName,
                AppName = record.AppName,
                UsageDate = record.UsageDate,
                TotalForegroundMs = record.TotalForegroundMs,
                FirstUsedAt = record.FirstUsedAt,
                LastUsedAt = record.LastUsedAt,
                OpenCountEstimate = record.OpenCountEstimate,
                CreatedAt = clock.UtcNow
            });
        }

        return records.Count;
    }

    private async Task<int> UpsertDomainAccessesAsync(
        Device device,
        IReadOnlyList<DomainAccessRecord>? records,
        CancellationToken cancellationToken)
    {
        if (records is null || records.Count == 0) return 0;

        var clientIds = records.Select(x => x.LocalId).ToList();
        var existing = await db.DomainAccesses
            .Where(x => x.DeviceId == device.Id && clientIds.Contains(x.ClientRecordId))
            .ToDictionaryAsync(x => x.ClientRecordId, cancellationToken);
        var categories = await db.DomainCategories.ToDictionaryAsync(x => x.Name, StringComparer.OrdinalIgnoreCase, cancellationToken);

        foreach (var record in records)
        {
            var category = ResolveCategory(record.Category, categories);
            if (existing.TryGetValue(record.LocalId, out var access))
            {
                var becameSensitive = !alertFactory.IsSensitive(access.CategoryId) && alertFactory.IsSensitive(category.Id);
                SyncRecordMerger.Merge(access, record, category.Id);
                if (becameSensitive) AddAlert(alertFactory.CreateSensitiveCategoryAlert(device, category.Name, access.Id));
                continue;
            }

            access = CreateDomainAccess(device.Id, record, category.Id);
            db.DomainAccesses.Add(access);
            AddAlert(alertFactory.CreateSensitiveCategoryAlert(device, category.Name, access.Id));
        }

        return records.Count;
    }

    private async Task<int> AddNewBlockAttemptsAsync(
        Device device,
        IReadOnlyList<BlockAttemptRecord>? records,
        CancellationToken cancellationToken)
    {
        if (records is null || records.Count == 0) return 0;

        var clientIds = records.Select(x => x.LocalId).ToList();
        var existingIds = await db.BlockAttempts
            .Where(x => x.DeviceId == device.Id && clientIds.Contains(x.ClientRecordId))
            .Select(x => x.ClientRecordId)
            .ToListAsync(cancellationToken);
        var existing = existingIds.ToHashSet();

        foreach (var record in records.Where(x => !existing.Contains(x.LocalId)))
        {
            var attempt = new BlockAttempt
            {
                DeviceId = device.Id,
                ClientRecordId = record.LocalId,
                BlockingRuleId = record.RuleId,
                Domain = SyncRecordMerger.NormalizeDomain(record.Domain),
                IpAddress = record.IpAddress,
                Protocol = record.Protocol,
                Port = record.Port,
                AttemptedAt = record.AttemptedAt,
                ForegroundPackageName = record.ForegroundPackageName,
                CorrelationType = record.CorrelationType
            };
            db.BlockAttempts.Add(attempt);
            AddAlert(alertFactory.CreateBlockAttemptAlert(device, attempt.Id));
        }

        return records.Count;
    }

    private DomainAccess CreateDomainAccess(Guid deviceId, DomainAccessRecord record, Guid categoryId) => new()
    {
        DeviceId = deviceId,
        ClientRecordId = record.LocalId,
        Domain = SyncRecordMerger.NormalizeDomain(record.Domain),
        IpAddress = record.IpAddress,
        Protocol = record.Protocol,
        Port = record.Port,
        CategoryId = categoryId,
        FirstAccessAt = record.FirstAccessAt,
        LastAccessAt = record.LastAccessAt,
        AccessCount = record.AccessCount,
        ForegroundPackageName = record.ForegroundPackageName,
        CorrelationType = record.CorrelationType,
        Source = record.Source,
        CreatedAt = clock.UtcNow
    };

    private void AddAlert(Alert? alert)
    {
        if (alert is not null) db.Alerts.Add(alert);
    }

    private SyncBatchResponse ToResponse(SyncBatch batch, Device device) =>
        new(batch.Id, batch.Status, batch.RecordsAccepted, clock.UtcNow, device.Config?.ConfigVersion ?? 1);

    private static DomainCategory ResolveCategory(
        string? categoryName,
        IReadOnlyDictionary<string, DomainCategory> categories)
    {
        if (categoryName is not null && categories.TryGetValue(categoryName, out var category)) return category;
        return categories["unknown"];
    }

}
