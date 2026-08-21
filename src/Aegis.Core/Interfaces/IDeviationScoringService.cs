using Aegis.Core.Models;

namespace Aegis.Core.Interfaces;

public interface IDeviationScoringService
{
    /// <summary>
    /// Scores a newly-ingested biometric reading against the astronaut's personal baseline.
    /// Updates the baseline via Welford's algorithm as a side-effect.
    /// Returns a CompositeRiskScore whose Score is the sum of tier weights across all metrics.
    /// </summary>
    Task<CompositeRiskScore> ScoreAsync(BiometricReading reading, CancellationToken ct = default);
}
