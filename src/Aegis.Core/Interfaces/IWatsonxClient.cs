using Aegis.Core.Models;

namespace Aegis.Core.Interfaces;

public interface IWatsonxClient
{
    /// <summary>
    /// Calls the watsonx.ai text generation API and returns a structured intervention plan
    /// for the given astronaut based on their triggering biometric readings.
    /// </summary>
    Task<InterventionPlanResult> GenerateInterventionPlanAsync(
        Astronaut astronaut,
        IEnumerable<BiometricReading> triggeringReadings,
        CancellationToken ct = default);
}
