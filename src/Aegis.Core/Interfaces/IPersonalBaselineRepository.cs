using Aegis.Core.Enums;
using Aegis.Core.Models;

namespace Aegis.Core.Interfaces;

public interface IPersonalBaselineRepository
{
    Task<PersonalBaseline?> GetAsync(Guid astronautId, MetricType metric, CancellationToken ct = default);
    Task AddAsync(PersonalBaseline baseline, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
