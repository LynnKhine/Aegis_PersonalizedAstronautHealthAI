using Aegis.Core.Enums;
using Aegis.Core.Models;

namespace Aegis.Api.Models;

public sealed record MissionLogEntry(
    Guid        PlanId,
    DateTime    GeneratedAt,
    int         CompositeScore,
    string      Summary,
    string[]    ImmediateActions,
    string      MonitoringFrequency,
    bool        EscalateToFlightSurgeon,
    ContributorEntry[] Contributors
);
