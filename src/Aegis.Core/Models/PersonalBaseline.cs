using Aegis.Core.Enums;

namespace Aegis.Core.Models;

public class PersonalBaseline
{
    public Guid Id { get; set; }
    public Guid AstronautId { get; set; }
    public MetricType MetricType { get; set; }
    public double Mean { get; set; }
    public double StdDev { get; set; }

    /// <summary>
    /// Running sum of squared deviations from the mean (M2 in Welford's algorithm).
    /// StdDev is recomputed as sqrt(SumOfSquaredDeviations / (SampleCount - 1)).
    /// </summary>
    public double SumOfSquaredDeviations { get; set; }
    public int SampleCount { get; set; }
    public DateTime LastUpdated { get; set; }

    public Astronaut Astronaut { get; set; } = null!;
}
