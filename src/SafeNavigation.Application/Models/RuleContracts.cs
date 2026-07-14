namespace SafeNavigation.Application.Models;

public sealed record BlockingRuleDto(
    Guid Id,
    string RuleType,
    string Value,
    string Action,
    int Priority,
    bool Enabled,
    Guid? ChildId,
    Guid? DeviceId);

public sealed record CreateRuleRequest(
    string RuleType,
    string Value,
    string Action,
    Guid? ChildId,
    Guid? DeviceId);

public sealed record UpdateRuleRequest(
    string RuleType,
    string Value,
    string Action,
    int Priority,
    bool Enabled,
    Guid? ChildId,
    Guid? DeviceId);

public sealed record CreateUnblockRequest(string Domain, string? Message);

public sealed record UnblockDecisionRequest(string Decision, string? Reason);

public sealed record UnblockRequestDto(
    Guid Id,
    Guid DeviceId,
    string Domain,
    string? Message,
    string Status,
    string? DecisionReason,
    DateTimeOffset CreatedAt,
    DateTimeOffset? DecidedAt);
