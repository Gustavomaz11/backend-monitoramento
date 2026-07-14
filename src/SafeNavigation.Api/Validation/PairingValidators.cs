using FluentValidation;
using SafeNavigation.Application.Models;

namespace SafeNavigation.Api.Validation;

public sealed class CreatePairingCodeRequestValidator : AbstractValidator<CreatePairingCodeRequest>
{
    public CreatePairingCodeRequestValidator()
    {
        RuleFor(x => x.ChildDisplayName).NotEmpty().MaximumLength(120);
        RuleFor(x => x.DeviceName).MaximumLength(120);
    }
}

public sealed class CompletePairingRequestValidator : AbstractValidator<CompletePairingRequest>
{
    public CompletePairingRequestValidator()
    {
        RuleFor(x => x.PairingCode).NotEmpty().Length(8);
        RuleFor(x => x.DeviceInfo).NotNull();
        RuleFor(x => x.DeviceInfo.AppVersion).MaximumLength(80);
        RuleFor(x => x.DeviceInfo.AndroidVersion).MaximumLength(80);
        RuleFor(x => x.DeviceInfo.Manufacturer).MaximumLength(120);
        RuleFor(x => x.DeviceInfo.Model).MaximumLength(120);
    }
}
