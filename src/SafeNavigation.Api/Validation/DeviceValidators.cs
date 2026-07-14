using FluentValidation;
using SafeNavigation.Application.Models;

namespace SafeNavigation.Api.Validation;

public sealed class DeviceConfigDtoValidator : AbstractValidator<DeviceConfigDto>
{
    public DeviceConfigDtoValidator()
    {
        RuleFor(x => x.RetentionDays).InclusiveBetween(1, 365);
        RuleFor(x => x.SyncIntervalMinutes).InclusiveBetween(15, 1440);
        RuleFor(x => x.Timezone).NotEmpty().MaximumLength(120);
    }
}
