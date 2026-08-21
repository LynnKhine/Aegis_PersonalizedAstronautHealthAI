using Aegis.Core.Models;
using Microsoft.AspNetCore.SignalR;

namespace Aegis.Api.Hubs;

/// <summary>
/// Richer SignalR push payload that carries both the plan fields and the
/// parsed explainability breakdown — so JS doesn't have to parse JSON-in-JSON.
/// </summary>
public sealed class InterventionPlanPush
{
    public Guid   Id                      { get; init; }
    public Guid   AstronautId             { get; init; }
    public string Summary                 { get; init; } = string.Empty;
    public string[] ImmediateActions      { get; init; } = [];
    public string MonitoringFrequency     { get; init; } = string.Empty;
    public bool   EscalateToFlightSurgeon { get; init; }
    public DateTime GeneratedAt           { get; init; }
    public int    CompositeScore          { get; init; }

    /// <summary>Per-metric Z-score breakdown that drove the composite score.</summary>
    public ContributorEntry[] Contributors { get; init; } = [];
}

/// <summary>Strongly-typed client contract for the Aegis SignalR hub.</summary>
public interface IAegisClient
{
    Task InterventionPlanGenerated(InterventionPlanPush push);
}

/// <summary>
/// SignalR hub.
/// Clients call JoinAstronautGroup(astronautId) after connecting.
/// The server pushes InterventionPlanGenerated to the matching group.
/// Group name convention: "astronaut-{astronautId}"
/// </summary>
public sealed class AegisHub : Hub<IAegisClient>
{
    public async Task JoinAstronautGroup(string astronautId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"astronaut-{astronautId}");
    }

    public async Task LeaveAstronautGroup(string astronautId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"astronaut-{astronautId}");
    }
}
