namespace SafeNavigation.Application.Abstractions;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
