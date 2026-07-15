using SafeNavigation.Application.Abstractions;
using SafeNavigation.Domain.Entities;

namespace SafeNavigation.Application.Services;

public sealed class SyncAlertFactory(IClock clock)
{
    private static readonly IReadOnlyDictionary<string, string> SensitiveAlertTypes =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["adult"] = "adult",
            ["gambling"] = "gambling",
            ["violence"] = "violence",
            ["malicious"] = "malicious"
        };

    private static readonly HashSet<Guid> SensitiveCategoryIds =
    [
        Guid.Parse("aaaaaaaa-0000-0000-0000-000000000006"),
        Guid.Parse("aaaaaaaa-0000-0000-0000-000000000007"),
        Guid.Parse("aaaaaaaa-0000-0000-0000-000000000009"),
        Guid.Parse("aaaaaaaa-0000-0000-0000-000000000010")
    ];

    public bool IsSensitive(Guid? categoryId) => categoryId is not null && SensitiveCategoryIds.Contains(categoryId.Value);

    public Alert? CreateSensitiveCategoryAlert(Device device, string categoryName, Guid relatedEntityId)
    {
        if (device.Child is null || !SensitiveAlertTypes.TryGetValue(categoryName, out var alertType)) return null;

        return new Alert
        {
            GuardianId = device.Child.GuardianId,
            ChildId = device.ChildId,
            DeviceId = device.Id,
            AlertType = alertType,
            Severity = "critical",
            Title = "Categoria sensivel detectada",
            Summary = $"Acesso classificado como {categoryName}. Apenas metadados foram registrados.",
            RelatedEntityType = "domain_access",
            RelatedEntityId = relatedEntityId,
            CreatedAt = clock.UtcNow
        };
    }

    public Alert? CreateBlockAttemptAlert(Device device, Guid relatedEntityId)
    {
        if (device.Child is null) return null;

        return new Alert
        {
            GuardianId = device.Child.GuardianId,
            ChildId = device.ChildId,
            DeviceId = device.Id,
            AlertType = "manual_block",
            Severity = "warning",
            Title = "Tentativa bloqueada",
            Summary = "Uma regra familiar bloqueou uma tentativa de acesso.",
            RelatedEntityType = "block_attempt",
            RelatedEntityId = relatedEntityId,
            CreatedAt = clock.UtcNow
        };
    }
}
