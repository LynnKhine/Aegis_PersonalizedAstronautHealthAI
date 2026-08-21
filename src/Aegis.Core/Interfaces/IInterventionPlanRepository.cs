using Aegis.Core.Models;

namespace Aegis.Core.Interfaces;

public interface IInterventionPlanRepository
{
    Task AddAsync(InterventionPlan plan, CancellationToken ct = default);
    Task<IReadOnlyList<InterventionPlan>> GetByAstronautAsync(Guid astronautId, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
