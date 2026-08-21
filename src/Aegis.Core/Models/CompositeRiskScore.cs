namespace Aegis.Core.Models;

/// <summary>
/// Result of deviation scoring for a new biometric reading.
/// Score = sum of SeverityLevel tier weights across the most-recent reading
/// per metric for this astronaut. Escalate to watsonx when Score >= 2.
/// </summary>
public record CompositeRiskScore(int Score, IReadOnlyList<BiometricReading> Contributors);
