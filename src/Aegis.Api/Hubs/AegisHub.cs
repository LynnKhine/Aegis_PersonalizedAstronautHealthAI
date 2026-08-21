using Aegis.Core.Models;
using Microsoft.AspNetCore.SignalR;

namespace Aegis.Api.Hubs;

/// <summary>Strongly-typed client contract for the Aegis SignalR hub.</summary>
public interface IAegisClient
{
    Task InterventionPlanGenerated(InterventionPlan plan);
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
