using SafeNavigation.Application.Abstractions;

namespace SafeNavigation.Infrastructure.Security;

public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
