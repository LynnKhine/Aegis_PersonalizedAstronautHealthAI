using Aegis.Core.Enums;
using Aegis.Core.Models;

namespace Aegis.Core.Interfaces;

public interface IBiometricReadingRepository
{
    Task AddAsync(BiometricReading reading, CancellationToken ct = default);
    Task<IReadOnlyList<BiometricReading>> GetByAstronautAsync(Guid astronautId, CancellationToken ct = default);
    Task<IReadOnlyList<BiometricReading>> GetByAstronautAndMetricAsync(Guid astronautId, MetricType metric, CancellationToken ct = default);

    /// <summary>Returns the single most-recent reading for each metric for the given astronaut.</summary>
    Task<IReadOnlyList<BiometricReading>> GetLatestPerMetricAsync(Guid astronautId, CancellationToken ct = default);

    Task<IReadOnlyList<BiometricReading>> GetPagedAsync(Guid astronautId, MetricType? metric, int page, int pageSize, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
