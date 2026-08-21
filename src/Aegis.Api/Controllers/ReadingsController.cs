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
    private readonly IAstronautRepository        _astronauts;
    private readonly IBiometricReadingRepository  _readings;
    private readonly IInterventionPlanRepository  _plans;
    private readonly IPersonalBaselineRepository  _baselineRepo;
    private readonly IDeviationScoringService     _scoring;
    private readonly IWatsonxClient               _watsonx;
    private readonly IHubContext<AegisHub, IAegisClient> _hub;

    public ReadingsController(
        IAstronautRepository astronauts,
        IBiometricReadingRepository readings,
        IInterventionPlanRepository plans,
        IPersonalBaselineRepository baselineRepo,
        IDeviationScoringService scoring,
        IWatsonxClient watsonx,
        IHubContext<AegisHub, IAegisClient> hub)
    {
        _astronauts   = astronauts;
        _readings     = readings;
        _plans        = plans;
        _baselineRepo = baselineRepo;
        _scoring      = scoring;
        _watsonx      = watsonx;
        _hub          = hub;
    }

    /// <summary>
    /// POST /api/readings/inject — dashboard control panel.
    /// Calculates a value that lands in the requested severity band based on
    /// the astronaut's live baseline, then delegates to IngestReading.
    /// </summary>
    [HttpPost("inject")]
    public async Task<IActionResult> InjectAnomaly(
        [FromBody] InjectAnomalyRequest request,
        CancellationToken ct)
    {
        var astronaut = await _astronauts.GetByIdAsync(request.AstronautId, ct);
        if (astronaut is null)
            return NotFound($"Astronaut {request.AstronautId} not found.");

        double value;

        if (request.Preset == "custom")
        {
            if (request.Value is null)
                return BadRequest("Value is required for custom preset.");
            value = request.Value.Value;
        }
        else
        {
            // Load the astronaut's baseline for this metric to compute a
            // realistic value that lands in the requested severity band.
            var baseline = await _baselineRepo.GetAsync(request.AstronautId, request.MetricType, ct);

            double mean   = baseline?.Mean   ?? MetricMidpoint(request.MetricType);
            double stdDev = (baseline is { StdDev: > 0 }) ? baseline.StdDev : MetricStdEstimate(request.MetricType);

            double sigmas = request.Preset == "severe"
                ? 2.6 + (Random.Shared.NextDouble() * 0.6)   // 2.6 – 3.2σ  → Alert/Critical
                : 1.7 + (Random.Shared.NextDouble() * 0.3);  // 1.7 – 2.0σ  → Warning

            // Inject below the mean (lower values = worse for most metrics)
            value = mean - sigmas * stdDev;
        }

        // Delegate to the standard ingest pipeline
        var ingestReq = new IngestReadingRequest(
            request.AstronautId,
            request.MetricType,
            value,
            DateTime.UtcNow);

        return await IngestReading(ingestReq, ct);
    }

    private static double MetricMidpoint(MetricType m) => m switch
    {
        MetricType.HRV              => 60.0,
        MetricType.SleepQuality     => 6.5,
        MetricType.BoneDensityIndex => 1.0,
        MetricType.MoodStressScore  => 5.5,
        _                           => 5.0,
    };

    private static double MetricStdEstimate(MetricType m) => m switch
    {
        MetricType.HRV              => 6.0,
        MetricType.SleepQuality     => 1.0,
        MetricType.BoneDensityIndex => 0.05,
        MetricType.MoodStressScore  => 1.5,
        _                           => 1.0,
    };

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
