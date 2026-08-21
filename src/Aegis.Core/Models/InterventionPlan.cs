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

    public Astronaut Astronaut { get; set; } = null!;
    public BiometricReading TriggeredByReading { get; set; } = null!;
}
