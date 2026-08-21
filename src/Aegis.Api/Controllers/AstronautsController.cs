using System.Text.Json;
using Aegis.Api.Models;
using Aegis.Core.Enums;
using Aegis.Core.Interfaces;
using Aegis.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace Aegis.Api.Controllers;

[ApiController]
[Route("api/astronauts")]
public sealed class AstronautsController : ControllerBase
{
    private readonly IAstronautRepository        _astronauts;
    private readonly IBiometricReadingRepository _readings;
    private readonly IInterventionPlanRepository _plans;

    public AstronautsController(
        IAstronautRepository astronauts,
        IBiometricReadingRepository readings,
        IInterventionPlanRepository plans)
    {
        _astronauts = astronauts;
        _readings   = readings;
        _plans      = plans;
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
            astronaut.MissionStartDate,
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

    /// <summary>GET /api/astronauts/{id}/interventions — mission log.</summary>
    [HttpGet("{id:guid}/interventions")]
    public async Task<IActionResult> GetInterventions(Guid id, CancellationToken ct)
    {
        if (await _astronauts.GetByIdAsync(id, ct) is null) return NotFound();

        var plans = await _plans.GetByAstronautAsync(id, ct);

        var entries = plans.Select(p =>
        {
            string[] actions = [];
            ContributorEntry[] contributors = [];
            try { actions = JsonSerializer.Deserialize<string[]>(p.ImmediateActionsJson) ?? []; } catch { }
            try { contributors = JsonSerializer.Deserialize<ContributorEntry[]>(p.ContributorsJson) ?? []; } catch { }

            return new MissionLogEntry(
                p.Id,
                p.GeneratedAt,
                p.CompositeScore,
                p.Summary,
                actions,
                p.MonitoringFrequency,
                p.EscalateToFlightSurgeon,
                contributors);
        }).ToList();

        return Ok(entries);
    }
}
