namespace Aegis.Core.Models;

public class InterventionPlan
{
    public Guid Id { get; set; }
    public Guid AstronautId { get; set; }
    public Guid TriggeredByReadingId { get; set; }
    public string Summary { get; set; } = string.Empty;

    /// <summary>JSON-serialized string[]. Stored as JSON text in SQLite.</summary>
    public string ImmediateActionsJson { get; set; } = "[]";
    public string MonitoringFrequency { get; set; } = string.Empty;
    public bool EscalateToFlightSurgeon { get; set; }
    public DateTime GeneratedAt { get; set; }

    /// <summary>
    /// JSON-serialized ContributorEntry[]. Stores the per-metric Z-score breakdown
    /// that drove the composite score — the explainability record for why the AI was called.
    /// e.g. [{"metric":"HRV","zScore":2.3,"severity":"Alert","tierWeight":2}, ...]
    /// </summary>
    public string ContributorsJson { get; set; } = "[]";

    /// <summary>Composite risk score at the time the plan was generated.</summary>
    public int CompositeScore { get; set; }

    public Astronaut Astronaut { get; set; } = null!;
    public BiometricReading TriggeredByReading { get; set; } = null!;
}
