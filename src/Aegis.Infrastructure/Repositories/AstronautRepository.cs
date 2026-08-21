using Aegis.Core.Interfaces;
using Aegis.Core.Models;
using Aegis.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Aegis.Infrastructure.Repositories;

public sealed class AstronautRepository : IAstronautRepository
{
    private readonly AegisDbContext _db;
    public AstronautRepository(AegisDbContext db) => _db = db;

    public Task<Astronaut?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.Astronauts.FirstOrDefaultAsync(a => a.Id == id, ct);

    public async Task<IReadOnlyList<Astronaut>> GetAllAsync(CancellationToken ct = default) =>
        await _db.Astronauts.ToListAsync(ct);

    public async Task AddAsync(Astronaut astronaut, CancellationToken ct = default) =>
        await _db.Astronauts.AddAsync(astronaut, ct);

    public Task SaveChangesAsync(CancellationToken ct = default) =>
        _db.SaveChangesAsync(ct);
}
