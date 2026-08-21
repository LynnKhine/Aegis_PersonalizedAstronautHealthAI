using Aegis.Core.Models;

namespace Aegis.Core.Interfaces;

public interface IAstronautRepository
{
    Task<Astronaut?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Astronaut>> GetAllAsync(CancellationToken ct = default);
    Task AddAsync(Astronaut astronaut, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
