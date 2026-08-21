using Aegis.Core.Enums;

namespace Aegis.Api.Models;

public sealed record AstronautStatusResponse(
    Guid AstronautId,
    string Name,
    string NASAId,
    DateTime MissionStartDate,
    int CompositeRiskScore,
    IReadOnlyList<LatestMetricReading> LatestReadings
);

public sealed record LatestMetricReading(
    MetricType MetricType,
    double Value,
    double ZScore,
    SeverityLevel Severity,
    DateTime RecordedAt
);
