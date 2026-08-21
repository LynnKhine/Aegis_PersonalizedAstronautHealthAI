using Aegis.Core.Enums;

namespace Aegis.Api.Models;

/// <summary>Request body for POST /api/readings/inject</summary>
public sealed record InjectAnomalyRequest(
    Guid AstronautId,
    MetricType MetricType,
    /// <summary>
    /// "mild"   → lands in Warning band  (1.7 – 2.0σ from baseline mean)
    /// "severe" → lands in Alert/Critical band (2.5 – 3.2σ)
    /// "custom" → uses Value directly
    /// </summary>
    string Preset,
    double? Value = null
);
