using Aegis.Core.Enums;
using Aegis.Core.Interfaces;
using Aegis.Core.Models;

namespace Aegis.Core.Services;

/// <summary>
/// Scores a new biometric reading against the astronaut's personal baseline.
///
/// Algorithm:
///   1. Load or create the PersonalBaseline for this astronaut + metric.
///   2. Compute Z-score = (value - mean) / stdDev.
///      Cold-start guard: if SampleCount < 2 or StdDev == 0 → Severity = None.
///   3. Map |Z-score| to SeverityLevel tier and tier weight.
///   4. Update the baseline using Welford's online algorithm (no history re-scan).
///   5. Fetch the most-recent reading per metric for this astronaut.
///   6. Composite score = sum of tier weights (including this new reading's weight).
///   7. Return CompositeRiskScore.
/// </summary>
public sealed class DeviationScoringService : IDeviationScoringService
{
    private readonly IPersonalBaselineRepository _baselines;
    private readonly IBiometricReadingRepository _readings;

    public DeviationScoringService(
        IPersonalBaselineRepository baselines,
        IBiometricReadingRepository readings)
    {
        _baselines = baselines;
        _readings  = readings;
    }

    public async Task<CompositeRiskScore> ScoreAsync(BiometricReading reading, CancellationToken ct = default)
    {
        // 1. Load or initialise baseline
        var baseline = await _baselines.GetAsync(reading.AstronautId, reading.MetricType, ct);
        if (baseline is null)
        {
            baseline = new PersonalBaseline
            {
                Id           = Guid.NewGuid(),
                AstronautId  = reading.AstronautId,
                MetricType   = reading.MetricType,
                Mean         = 0,
                StdDev       = 0,
                SumOfSquaredDeviations = 0,
                SampleCount  = 0,
                LastUpdated  = reading.RecordedAt
            };
            await _baselines.AddAsync(baseline, ct);
        }

        // 2. Compute Z-score (cold-start guard)
        double zScore   = 0;
        SeverityLevel severity = SeverityLevel.None;

        if (baseline.SampleCount >= 2 && baseline.StdDev > 0)
        {
            zScore   = Math.Abs((reading.Value - baseline.Mean) / baseline.StdDev);
            severity = MapToSeverity(zScore);
        }

        reading.ZScore   = zScore;
        reading.Severity = severity;

        // 3. Update baseline — Welford's online algorithm
        baseline.SampleCount++;
        double delta    = reading.Value - baseline.Mean;
        baseline.Mean  += delta / baseline.SampleCount;
        double delta2   = reading.Value - baseline.Mean;   // uses NEW mean
        baseline.SumOfSquaredDeviations += delta * delta2;

        if (baseline.SampleCount >= 2)
        {
            baseline.StdDev = Math.Sqrt(baseline.SumOfSquaredDeviations / (baseline.SampleCount - 1));
        }

        baseline.LastUpdated = reading.RecordedAt;
        await _baselines.SaveChangesAsync(ct);

        // 4. Fetch most-recent reading per metric (already persisted by caller before ScoreAsync)
        var latestPerMetric = await _readings.GetLatestPerMetricAsync(reading.AstronautId, ct);

        // 5. Build contributor list: replace any existing entry for this metric with the new reading
        var contributors = latestPerMetric
            .Where(r => r.MetricType != reading.MetricType)
            .Concat(new[] { reading })
            .ToList();

        // 6. Composite score = sum of tier weights
        int compositeScore = contributors.Sum(r => TierWeight(r.Severity));

        return new CompositeRiskScore(compositeScore, contributors.AsReadOnly());
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static SeverityLevel MapToSeverity(double absZScore) => absZScore switch
    {
        >= 3.0 => SeverityLevel.Critical,
        >= 2.0 => SeverityLevel.Alert,
        >= 1.5 => SeverityLevel.Warning,
        _      => SeverityLevel.None
    };

    private static int TierWeight(SeverityLevel severity) => severity switch
    {
        SeverityLevel.Warning  => 1,
        SeverityLevel.Alert    => 2,
        SeverityLevel.Critical => 3,
        _                      => 0
    };
}
