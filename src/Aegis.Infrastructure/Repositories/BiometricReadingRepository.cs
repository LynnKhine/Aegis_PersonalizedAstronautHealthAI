using Aegis.Core.Enums;
using Aegis.Core.Interfaces;
using Aegis.Core.Models;
using Aegis.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Aegis.Infrastructure.Repositories;

public sealed class BiometricReadingRepository : IBiometricReadingRepository
{
    private readonly AegisDbContext _db;
    public BiometricReadingRepository(AegisDbContext db) => _db = db;

    public async Task AddAsync(BiometricReading reading, CancellationToken ct = default) =>
        await _db.BiometricReadings.AddAsync(reading, ct);

    public async Task<IReadOnlyList<BiometricReading>> GetByAstronautAsync(
        Guid astronautId, CancellationToken ct = default) =>
        await _db.BiometricReadings
            .Where(r => r.AstronautId == astronautId)
            .OrderByDescending(r => r.RecordedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<BiometricReading>> GetByAstronautAndMetricAsync(
        Guid astronautId, MetricType metric, CancellationToken ct = default) =>
        await _db.BiometricReadings
            .Where(r => r.AstronautId == astronautId && r.MetricType == metric)
            .OrderByDescending(r => r.RecordedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<BiometricReading>> GetLatestPerMetricAsync(
        Guid astronautId, CancellationToken ct = default)
    {
        // Fetch the most-recent reading for each metric via a client-side grouping.
        // The dataset is small (4 metrics) so this is perfectly fine.
        var readings = await _db.BiometricReadings
            .Where(r => r.AstronautId == astronautId)
            .OrderByDescending(r => r.RecordedAt)
            .ToListAsync(ct);

        return readings
            .GroupBy(r => r.MetricType)
            .Select(g => g.First())
            .ToList();
    }

    public async Task<IReadOnlyList<BiometricReading>> GetPagedAsync(
        Guid astronautId, MetricType? metric, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _db.BiometricReadings.Where(r => r.AstronautId == astronautId);
        if (metric.HasValue)
            query = query.Where(r => r.MetricType == metric.Value);

        return await query
            .OrderByDescending(r => r.RecordedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
    }

    public Task SaveChangesAsync(CancellationToken ct = default) =>
        _db.SaveChangesAsync(ct);
}
