using FluentValidation;
using SafeNavigation.Application.Models;

namespace SafeNavigation.Api.Validation;

public sealed class CreateRuleRequestValidator : AbstractValidator<CreateRuleRequest>
{
    private static readonly string[] RuleTypes = ["domain", "category", "schedule", "allow_domain"];
    private static readonly string[] Actions = ["block", "allow", "alert_only"];

    public CreateRuleRequestValidator()
    {
        RuleFor(x => x.RuleType).Must(x => RuleTypes.Contains(x));
        RuleFor(x => x.Value).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Action).Must(x => Actions.Contains(x));
    }
}

public sealed class UpdateRuleRequestValidator : AbstractValidator<UpdateRuleRequest>
{
    private static readonly string[] RuleTypes = ["domain", "category", "schedule", "allow_domain"];
    private static readonly string[] Actions = ["block", "allow", "alert_only"];

    public UpdateRuleRequestValidator()
    {
        RuleFor(x => x.RuleType).Must(x => RuleTypes.Contains(x));
        RuleFor(x => x.Value).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Action).Must(x => Actions.Contains(x));
        RuleFor(x => x.Priority).InclusiveBetween(1, 1000);
    }
}

public sealed class CreateUnblockRequestValidator : AbstractValidator<CreateUnblockRequest>
{
    public CreateUnblockRequestValidator()
    {
        RuleFor(x => x.Domain).NotEmpty().MaximumLength(253);
        RuleFor(x => x.Message).MaximumLength(500);
    }
}

public sealed class UnblockDecisionRequestValidator : AbstractValidator<UnblockDecisionRequest>
{
    public UnblockDecisionRequestValidator()
    {
        RuleFor(x => x.Decision).Must(x => x is "approved" or "denied");
        RuleFor(x => x.Reason).MaximumLength(500);
    }
}
