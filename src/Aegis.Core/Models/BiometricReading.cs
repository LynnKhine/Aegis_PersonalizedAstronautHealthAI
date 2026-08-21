using Aegis.Core.Enums;

namespace Aegis.Core.Models;

public class BiometricReading
{
    public Guid Id { get; set; }
    public Guid AstronautId { get; set; }
    public MetricType MetricType { get; set; }
    public double Value { get; set; }
    public DateTime RecordedAt { get; set; }
    public double ZScore { get; set; }
    public SeverityLevel Severity { get; set; }

    public Astronaut Astronaut { get; set; } = null!;
}
