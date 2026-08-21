using Aegis.Core.Interfaces;
using Aegis.Core.Models;
using Aegis.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Aegis.Infrastructure.Repositories;

public sealed class InterventionPlanRepository : IInterventionPlanRepository
{
    private readonly AegisDbContext _db;
    public InterventionPlanRepository(AegisDbContext db) => _db = db;

    public async Task AddAsync(InterventionPlan plan, CancellationToken ct = default) =>
        await _db.InterventionPlans.AddAsync(plan, ct);

    public async Task<IReadOnlyList<InterventionPlan>> GetByAstronautAsync(
        Guid astronautId, CancellationToken ct = default) =>
        await _db.InterventionPlans
            .Where(p => p.AstronautId == astronautId)
            .OrderByDescending(p => p.GeneratedAt)
            .ToListAsync(ct);

    public Task SaveChangesAsync(CancellationToken ct = default) =>
        _db.SaveChangesAsync(ct);
}
