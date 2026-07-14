using FluentValidation;
using SafeNavigation.Application.Models;

namespace SafeNavigation.Api.Validation;

public sealed class UpdateAlertStatusRequestValidator : AbstractValidator<UpdateAlertStatusRequest>
{
    public UpdateAlertStatusRequestValidator()
    {
        RuleFor(x => x.Status).Must(x => x is "new" or "read" or "resolved");
    }
}
