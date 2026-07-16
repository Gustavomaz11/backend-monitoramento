using System.Text.Json;
using SafeNavigation.Application.Models;

namespace SafeNavigation.Application.Services;

internal static class UsageScheduleSerialization
{
    public static IReadOnlyList<DailyUsageWindowDto> Read(string json)
    {
        var parsed = JsonSerializer.Deserialize<List<DailyUsageWindowDto>>(json) ?? [];
        if (parsed.Count == 7) return parsed.OrderBy(x => x.DayOfWeek).ToArray();
        return Unrestricted();
    }

    public static string Write(IReadOnlyList<DailyUsageWindowDto> schedule) =>
        JsonSerializer.Serialize(schedule.OrderBy(x => x.DayOfWeek));

    private static IReadOnlyList<DailyUsageWindowDto> Unrestricted() =>
        Enumerable.Range(1, 7)
            .Select(day => new DailyUsageWindowDto(day, true, 0, 1440))
            .ToArray();
}
