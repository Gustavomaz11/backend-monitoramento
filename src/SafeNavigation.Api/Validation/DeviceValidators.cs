using FluentValidation;
using SafeNavigation.Application.Models;

namespace SafeNavigation.Api.Validation;

public sealed class DeviceConfigDtoValidator : AbstractValidator<DeviceConfigDto>
{
    public DeviceConfigDtoValidator()
    {
        RuleFor(x => x.RetentionDays).InclusiveBetween(1, 365);
        RuleFor(x => x.SyncIntervalMinutes).InclusiveBetween(15, 1440);
        RuleFor(x => x.Timezone)
            .NotEmpty()
            .MaximumLength(120)
            .Must(BeValidTimezone)
            .WithMessage("Timezone is invalid.");
        RuleFor(x => x.UsageSchedule)
            .NotNull()
            .Must(schedule => schedule.Count == 7 && schedule.Select(x => x.DayOfWeek).Distinct().Count() == 7)
            .WithMessage("Usage schedule must contain every day exactly once.");
        RuleForEach(x => x.UsageSchedule).ChildRules(window =>
        {
            window.RuleFor(x => x.DayOfWeek).InclusiveBetween(1, 7);
            window.RuleFor(x => x.StartMinute).InclusiveBetween(0, 1439);
            window.RuleFor(x => x.EndMinute).InclusiveBetween(1, 1440);
            window.RuleFor(x => x).Must(x => !x.Enabled || x.StartMinute < x.EndMinute)
                .WithMessage("Enabled windows must end after they start.");
        });
    }

    private static bool BeValidTimezone(string timezone)
    {
        try
        {
            _ = TimeZoneInfo.FindSystemTimeZoneById(timezone);
            return true;
        }
        catch (TimeZoneNotFoundException)
        {
            return false;
        }
        catch (InvalidTimeZoneException)
        {
            return false;
        }
    }
}
