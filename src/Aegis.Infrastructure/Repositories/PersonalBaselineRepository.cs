using Aegis.Core.Enums;
using Aegis.Core.Interfaces;
using Aegis.Core.Models;
using Aegis.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Aegis.Infrastructure.Repositories;

public sealed class PersonalBaselineRepository : IPersonalBaselineRepository
{
    private readonly AegisDbContext _db;
    public PersonalBaselineRepository(AegisDbContext db) => _db = db;

    public Task<PersonalBaseline?> GetAsync(Guid astronautId, MetricType metric, CancellationToken ct = default) =>
        _db.PersonalBaselines
            .FirstOrDefaultAsync(b => b.AstronautId == astronautId && b.MetricType == metric, ct);

    public async Task AddAsync(PersonalBaseline baseline, CancellationToken ct = default) =>
        await _db.PersonalBaselines.AddAsync(baseline, ct);

    public Task SaveChangesAsync(CancellationToken ct = default) =>
        _db.SaveChangesAsync(ct);
}
