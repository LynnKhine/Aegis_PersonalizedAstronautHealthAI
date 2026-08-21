namespace Aegis.Core.Models;

/// <summary>
/// Deserialized output from the watsonx.ai intervention plan generation call.
/// </summary>
public record InterventionPlanResult(
    string Summary,
    string[] ImmediateActions,
    string MonitoringFrequency,
    bool EscalateToFlightSurgeon
);
