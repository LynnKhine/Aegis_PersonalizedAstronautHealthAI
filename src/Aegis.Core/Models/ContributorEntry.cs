using Aegis.Core.Enums;

namespace Aegis.Core.Models;

/// <summary>
/// One row of the explainability breakdown — which metric, how many sigma,
/// what severity tier, and how much weight it contributed to the composite score.
/// </summary>
public record ContributorEntry(
    MetricType Metric,
    double     ZScore,
    SeverityLevel Severity,
    int        TierWeight
);
