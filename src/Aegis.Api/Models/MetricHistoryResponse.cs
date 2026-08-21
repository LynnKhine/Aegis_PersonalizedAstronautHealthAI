using Aegis.Core.Enums;

namespace Aegis.Api.Models;

public sealed record MetricHistoryResponse(
    MetricType MetricType,
    double? BaselineMean,
    double? BaselineStdDev,
    IReadOnlyList<MetricHistoryPoint> Points
);

public sealed record MetricHistoryPoint(
    double Value,
    double ZScore,
    SeverityLevel Severity,
    DateTime RecordedAt
);
