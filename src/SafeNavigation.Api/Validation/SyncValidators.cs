using FluentValidation;
using SafeNavigation.Application.Models;

namespace SafeNavigation.Api.Validation;

public sealed class SyncBatchRequestValidator : AbstractValidator<SyncBatchRequest>
{
    private static readonly string[] Protocols = ["http", "https", "tcp", "udp", "icmp", "unknown"];
    private static readonly string[] Correlations = ["none", "estimated", "observed"];
    private static readonly string[] Sources = ["browser_navigation", "dns", "sni_if_available", "ip_only", "manual", "unknown"];

    public SyncBatchRequestValidator()
    {
        RuleFor(x => x.ClientBatchId).NotEmpty();
        RuleFor(x => x.DeviceId).NotEmpty();
        RuleFor(x => x.OccurredTo).GreaterThanOrEqualTo(x => x.OccurredFrom);
        RuleFor(x => x.AppUsages).Must(HaveAtMost500Records);
        RuleFor(x => x.DomainAccesses).Must(HaveAtMost500Records);
        RuleFor(x => x.BlockAttempts).Must(HaveAtMost500Records);
        RuleFor(x => x.AppUsages).Must(x => HaveUniqueIds(x?.Select(y => y.LocalId)));
        RuleFor(x => x.DomainAccesses).Must(x => HaveUniqueIds(x?.Select(y => y.LocalId)));
        RuleFor(x => x.BlockAttempts).Must(x => HaveUniqueIds(x?.Select(y => y.LocalId)));
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
            domain.RuleFor(x => x.Domain).MaximumLength(253);
            domain.RuleFor(x => x.IpAddress).MaximumLength(64);
            domain.RuleFor(x => x.Port).InclusiveBetween(1, 65535).When(x => x.Port is not null);
            domain.RuleFor(x => x.LastAccessAt).GreaterThanOrEqualTo(x => x.FirstAccessAt);
        });
        RuleForEach(x => x.BlockAttempts).ChildRules(attempt =>
        {
            attempt.RuleFor(x => x.LocalId).NotEmpty();
            attempt.RuleFor(x => x.Protocol).Must(x => Protocols.Contains(x));
            attempt.RuleFor(x => x.CorrelationType).Must(x => Correlations.Contains(x));
            attempt.RuleFor(x => x.Domain).MaximumLength(253);
            attempt.RuleFor(x => x.IpAddress).MaximumLength(64);
            attempt.RuleFor(x => x.Port).InclusiveBetween(1, 65535).When(x => x.Port is not null);
        });
    }

    private static bool HaveAtMost500Records<T>(IReadOnlyList<T>? records) => records is null || records.Count <= 500;

    private static bool HaveUniqueIds(IEnumerable<Guid>? ids) => ids is null || ids.Distinct().Count() == ids.Count();
}
