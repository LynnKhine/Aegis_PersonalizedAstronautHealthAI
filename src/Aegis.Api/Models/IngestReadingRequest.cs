using Aegis.Core.Enums;

namespace Aegis.Api.Models;

public sealed record IngestReadingRequest(
    Guid AstronautId,
    MetricType MetricType,
    double Value,
    DateTime RecordedAt
);
