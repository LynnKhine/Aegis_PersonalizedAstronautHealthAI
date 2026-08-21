using System.Text.Json;
using Aegis.Api.Hubs;
using Aegis.Api.Models;
using Aegis.Core.Enums;
using Aegis.Core.Interfaces;
using Aegis.Core.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace Aegis.Api.Controllers;

[ApiController]
[Route("api/readings")]
public sealed class ReadingsController : ControllerBase
{
    private readonly IAstronautRepository       _astronauts;
    private readonly IBiometricReadingRepository _readings;
    private readonly IInterventionPlanRepository _plans;
    private readonly IDeviationScoringService    _scoring;
    private readonly IWatsonxClient              _watsonx;
    private readonly IHubContext<AegisHub, IAegisClient> _hub;

    public ReadingsController(
        IAstronautRepository astronauts,
        IBiometricReadingRepository readings,
        IInterventionPlanRepository plans,
        IDeviationScoringService scoring,
        IWatsonxClient watsonx,
        IHubContext<AegisHub, IAegisClient> hub)
    {
        _astronauts = astronauts;
        _readings   = readings;
        _plans      = plans;
        _scoring    = scoring;
        _watsonx    = watsonx;
        _hub        = hub;
    }

    /// <summary>POST /api/readings — ingest a new biometric reading.</summary>
    [HttpPost]
    public async Task<IActionResult> IngestReading(
        [FromBody] IngestReadingRequest request,
        CancellationToken ct)
    {
        var astronaut = await _astronauts.GetByIdAsync(request.AstronautId, ct);
        if (astronaut is null)
            return NotFound($"Astronaut {request.AstronautId} not found.");

        // 1. Persist the reading (severity/ZScore set by scoring service next)
        var reading = new BiometricReading
        {
            Id          = Guid.NewGuid(),
            AstronautId = request.AstronautId,
            MetricType  = request.MetricType,
            Value       = request.Value,
            RecordedAt  = request.RecordedAt,
            ZScore      = 0,
            Severity    = SeverityLevel.None,
        };
        await _readings.AddAsync(reading, ct);
        await _readings.SaveChangesAsync(ct);

        // 2. Score against personal baseline
        var composite = await _scoring.ScoreAsync(reading, ct);

        // 3. Conditionally escalate to watsonx
        InterventionPlanResponse? planResponse = null;

        if (composite.Score >= 2)
        {
            var result = await _watsonx.GenerateInterventionPlanAsync(
                astronaut, composite.Contributors, ct);

            var plan = new InterventionPlan
            {
                Id                    = Guid.NewGuid(),
                AstronautId           = astronaut.Id,
                TriggeredByReadingId  = reading.Id,
                Summary               = result.Summary,
                ImmediateActionsJson  = JsonSerializer.Serialize(result.ImmediateActions),
                MonitoringFrequency   = result.MonitoringFrequency,
                EscalateToFlightSurgeon = result.EscalateToFlightSurgeon,
                GeneratedAt           = DateTime.UtcNow,
            };

            await _plans.AddAsync(plan, ct);
            await _plans.SaveChangesAsync(ct);

            // 4. Push to astronaut's SignalR group
            await _hub.Clients
                .Group($"astronaut-{astronaut.Id}")
                .InterventionPlanGenerated(plan);

            planResponse = new InterventionPlanResponse(
                plan.Id,
                plan.Summary,
                result.ImmediateActions,
                plan.MonitoringFrequency,
                plan.EscalateToFlightSurgeon,
                plan.GeneratedAt);
        }

        var response = new ReadingResponse(
            reading.Id,
            reading.AstronautId,
            reading.MetricType,
            reading.Value,
            reading.RecordedAt,
            reading.ZScore,
            reading.Severity,
            composite.Score,
            planResponse);

        return CreatedAtAction(nameof(IngestReading), new { id = reading.Id }, response);
    }
}
