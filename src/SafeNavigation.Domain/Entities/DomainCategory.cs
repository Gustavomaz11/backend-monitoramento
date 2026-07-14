namespace SafeNavigation.Domain.Entities;

public sealed class DomainCategory
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public int RiskLevel { get; set; }
}
