using Aegis.Core.Enums;

namespace Aegis.Api.Models;

public sealed record ReadingResponse(
    Guid ReadingId,
    Guid AstronautId,
    MetricType MetricType,
    double Value,
    DateTime RecordedAt,
    double ZScore,
    SeverityLevel Severity,
    int CompositeRiskScore,
    InterventionPlanResponse? InterventionPlan
);
