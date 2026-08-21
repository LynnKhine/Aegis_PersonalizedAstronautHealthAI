namespace Aegis.Api.Models;

public sealed record InterventionPlanResponse(
    Guid PlanId,
    string Summary,
    string[] ImmediateActions,
    string MonitoringFrequency,
    bool EscalateToFlightSurgeon,
    DateTime GeneratedAt
);
