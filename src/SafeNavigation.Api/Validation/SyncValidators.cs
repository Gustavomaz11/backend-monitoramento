using FluentValidation;
using SafeNavigation.Application.Models;

namespace SafeNavigation.Api.Validation;

public sealed class SyncBatchRequestValidator : AbstractValidator<SyncBatchRequest>
{
    private static readonly string[] Protocols = ["tcp", "udp", "icmp", "unknown"];
    private static readonly string[] Correlations = ["none", "estimated", "observed"];
    private static readonly string[] Sources = ["dns", "sni_if_available", "ip_only", "manual", "unknown"];

    public SyncBatchRequestValidator()
    {
        RuleFor(x => x.ClientBatchId).NotEmpty();
        RuleFor(x => x.DeviceId).NotEmpty();
        RuleFor(x => x.OccurredTo).GreaterThanOrEqualTo(x => x.OccurredFrom);
        RuleForEach(x => x.AppUsages).ChildRules(app =>
        {
            app.RuleFor(x => x.LocalId).NotEmpty();
            app.RuleFor(x => x.PackageName).NotEmpty().MaximumLength(250);
            app.RuleFor(x => x.TotalForegroundMs).GreaterThanOrEqualTo(0);
        });
        RuleForEach(x => x.DomainAccesses).ChildRules(domain =>
        {
            domain.RuleFor(x => x.LocalId).NotEmpty();
            domain.RuleFor(x => x.Protocol).Must(x => Protocols.Contains(x));
            domain.RuleFor(x => x.CorrelationType).Must(x => Correlations.Contains(x));
            domain.RuleFor(x => x.Source).Must(x => Sources.Contains(x));
            domain.RuleFor(x => x.AccessCount).GreaterThan(0);
        });
        RuleForEach(x => x.BlockAttempts).ChildRules(attempt =>
        {
            attempt.RuleFor(x => x.LocalId).NotEmpty();
            attempt.RuleFor(x => x.Protocol).Must(x => Protocols.Contains(x));
            attempt.RuleFor(x => x.CorrelationType).Must(x => Correlations.Contains(x));
        });
    }
}
