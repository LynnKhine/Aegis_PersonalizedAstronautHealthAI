using Aegis.Api.Models;
using Aegis.Core.Enums;
using Aegis.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Aegis.Api.Controllers;

[ApiController]
[Route("api/astronauts")]
public sealed class AstronautsController : ControllerBase
{
    private readonly IAstronautRepository        _astronauts;
    private readonly IBiometricReadingRepository _readings;

    public AstronautsController(
        IAstronautRepository astronauts,
        IBiometricReadingRepository readings)
    {
        _astronauts = astronauts;
        _readings   = readings;
    }

    /// <summary>GET /api/astronauts/{id}/status</summary>
    [HttpGet("{id:guid}/status")]
    public async Task<IActionResult> GetStatus(Guid id, CancellationToken ct)
    {
        var astronaut = await _astronauts.GetByIdAsync(id, ct);
        if (astronaut is null) return NotFound();

        var latestPerMetric = await _readings.GetLatestPerMetricAsync(id, ct);

        // Composite score = sum of tier weights across latest readings
        int compositeScore = latestPerMetric.Sum(r => r.Severity switch
        {
            SeverityLevel.Warning  => 1,
            SeverityLevel.Alert    => 2,
            SeverityLevel.Critical => 3,
            _                      => 0
        });

        var latestReadings = latestPerMetric
            .Select(r => new LatestMetricReading(r.MetricType, r.Value, r.ZScore, r.Severity, r.RecordedAt))
            .ToList();

        return Ok(new AstronautStatusResponse(
            astronaut.Id,
            astronaut.Name,
            astronaut.NASAId,
            compositeScore,
            latestReadings));
    }

    /// <summary>GET /api/astronauts/{id}/readings?metric=HRV&amp;page=1</summary>
    [HttpGet("{id:guid}/readings")]
    public async Task<IActionResult> GetReadings(
        Guid id,
        [FromQuery] MetricType? metric,
        [FromQuery] int page = 1,
        CancellationToken ct = default)
    {
        if (await _astronauts.GetByIdAsync(id, ct) is null) return NotFound();

        const int pageSize = 50;
        var readings = await _readings.GetPagedAsync(id, metric, page, pageSize, ct);
        return Ok(readings);
    }
}
