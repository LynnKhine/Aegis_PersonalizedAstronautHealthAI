using System.Text.Json;
using Aegis.Api.Hubs;
using Aegis.Core.Enums;
using Aegis.Core.Interfaces;
using Aegis.Core.Models;
using Aegis.Core.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;

namespace Aegis.Api.Services;

/// <summary>
/// Background worker that generates one biometric reading per astronaut
/// per metric on a configurable cadence. Every ~20 ticks one metric gets
/// a mild anomaly; every ~60 ticks a more significant spike is injected
/// so judges watching the dashboard always see something happening.
/// </summary>
public sealed class LiveSimulationWorker : BackgroundService
{
    private static readonly Dictionary<MetricType, (double Min, double Max)> Ranges = new()
    {
        [MetricType.HRV]              = (40,   80),
        [MetricType.SleepQuality]     = (4.0,  9.0),
        [MetricType.BoneDensityIndex] = (0.85, 1.15),
        [MetricType.MoodStressScore]  = (1.0,  10.0),
    };

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHubContext<AegisHub, IAegisClient> _hub;
    private readonly ILogger<LiveSimulationWorker> _log;
    private readonly Random _rng = new();
    private int _tickCount = 0;

    public LiveSimulationWorker(
        IServiceScopeFactory scopeFactory,
        IHubContext<AegisHub, IAegisClient> hub,
        ILogger<LiveSimulationWorker> log)
    {
        _scopeFactory = scopeFactory;
        _hub          = hub;
        _log          = log;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        // Wait a few seconds after startup so migrations finish first
        await Task.Delay(TimeSpan.FromSeconds(5), ct);

        while (!ct.IsCancellationRequested)
        {
            _tickCount++;
            try { await TickAsync(ct); }
            catch (Exception ex) when (!ct.IsCancellationRequested)
            { _log.LogWarning(ex, "[LiveSim] Tick {T} failed", _tickCount); }

            await Task.Delay(TimeSpan.FromSeconds(10), ct);
        }
    }

    private async Task TickAsync(CancellationToken ct)
    {
        await using var scope     = _scopeFactory.CreateAsyncScope();
        var astronautRepo = scope.ServiceProvider.GetRequiredService<IAstronautRepository>();
        var readingRepo   = scope.ServiceProvider.GetRequiredService<IBiometricReadingRepository>();
        var planRepo      = scope.ServiceProvider.GetRequiredService<IInterventionPlanRepository>();
        var scoring       = scope.ServiceProvider.GetRequiredService<IDeviationScoringService>();
        var watsonx       = scope.ServiceProvider.GetRequiredService<IWatsonxClient>();

        var astronauts = await astronautRepo.GetAllAsync(ct);
        if (!astronauts.Any()) return;

        // Pick one random metric per astronaut this tick
        foreach (var astronaut in astronauts)
        {
            var metric   = (MetricType)_rng.Next(0, 4);
            var (min, max) = Ranges[metric];
            var midpoint = (min + max) / 2.0;
            var stdEstimate = (max - min) / 6.0; // rough estimate

            double value;

            // Every ~60 ticks: inject a significant spike (Alert/Critical territory)
            if (_tickCount % 61 == 0)
            {
                var sigmas = 2.2 + _rng.NextDouble() * 1.4; // 2.2 – 3.6σ
                value = midpoint - sigmas * stdEstimate;      // drop below mean
                value = Math.Clamp(value, min * 0.6, max);
                _log.LogInformation("[LiveSim] SPIKE — {Name} {Metric} = {Val:F2}", astronaut.Name, metric, value);
            }
            // Every ~20 ticks: mild anomaly (Warning territory)
            else if (_tickCount % 19 == 0)
            {
                var sigmas = 1.6 + _rng.NextDouble() * 0.3;
                value = midpoint - sigmas * stdEstimate;
                value = Math.Clamp(value, min * 0.75, max);
            }
            // Normal: small noise around midpoint
            else
            {
                var noise = (_rng.NextDouble() - 0.5) * (max - min) * 0.12;
                value = Math.Clamp(midpoint + noise, min, max);
            }

            var reading = new BiometricReading
            {
                Id          = Guid.NewGuid(),
                AstronautId = astronaut.Id,
                MetricType  = metric,
                Value       = value,
                RecordedAt  = DateTime.UtcNow,
                ZScore      = 0,
                Severity    = SeverityLevel.None,
            };

            await readingRepo.AddAsync(reading, ct);
            await readingRepo.SaveChangesAsync(ct);

            var composite = await scoring.ScoreAsync(reading, ct);

            if (composite.Score >= 2)
            {
                try
                {
                    var result = await watsonx.GenerateInterventionPlanAsync(
                        astronaut, composite.Contributors, ct);

                    var contributors = composite.Contributors
                        .Where(r => r.Severity != SeverityLevel.None)
                        .OrderByDescending(r => r.ZScore)
                        .Select(r => new ContributorEntry(
                            r.MetricType,
                            Math.Round(r.ZScore, 2),
                            r.Severity,
                            r.Severity switch {
                                SeverityLevel.Warning  => 1,
                                SeverityLevel.Alert    => 2,
                                SeverityLevel.Critical => 3,
                                _                      => 0 }))
                        .ToArray();

                    var plan = new InterventionPlan
                    {
                        Id                      = Guid.NewGuid(),
                        AstronautId             = astronaut.Id,
                        TriggeredByReadingId    = reading.Id,
                        Summary                 = result.Summary,
                        ImmediateActionsJson    = JsonSerializer.Serialize(result.ImmediateActions),
                        MonitoringFrequency     = result.MonitoringFrequency,
                        EscalateToFlightSurgeon = result.EscalateToFlightSurgeon,
                        GeneratedAt             = DateTime.UtcNow,
                        ContributorsJson        = JsonSerializer.Serialize(contributors),
                        CompositeScore          = composite.Score,
                    };

                    await planRepo.AddAsync(plan, ct);
                    await planRepo.SaveChangesAsync(ct);

                    var push = new InterventionPlanPush
                    {
                        Id                      = plan.Id,
                        AstronautId             = plan.AstronautId,
                        Summary                 = plan.Summary,
                        ImmediateActions        = result.ImmediateActions,
                        MonitoringFrequency     = plan.MonitoringFrequency,
                        EscalateToFlightSurgeon = plan.EscalateToFlightSurgeon,
                        GeneratedAt             = plan.GeneratedAt,
                        CompositeScore          = composite.Score,
                        Contributors            = contributors,
                    };

                    await _hub.Clients
                        .Group($"astronaut-{astronaut.Id}")
                        .InterventionPlanGenerated(push);
                }
                catch (Exception ex)
                {
                    _log.LogWarning(ex, "[LiveSim] watsonx call failed for {Name}", astronaut.Name);
                }
            }
        }
    }
}
