namespace SafeNavigation.Application.Models;

public sealed record AlertDto(
    Guid Id,
    string AlertType,
    string Severity,
    string Title,
    string Summary,
    string Status,
    DateTimeOffset CreatedAt);

public sealed record UpdateAlertStatusRequest(string Status);
