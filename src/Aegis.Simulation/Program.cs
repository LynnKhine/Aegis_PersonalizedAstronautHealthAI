using Aegis.Core.Enums;
using Aegis.Core.Interfaces;
using Aegis.Core.Models;
using Aegis.Core.Services;
using Aegis.Infrastructure.Data;
using Aegis.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

// ─── Configuration ────────────────────────────────────────────────────────────
var config = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false)
    .Build();

var connectionString = config.GetConnectionString("AegisDb")
    ?? "Data Source=aegis.db";

// ─── DI Container ─────────────────────────────────────────────────────────────
var services = new ServiceCollection();
services.AddAegisInfrastructure(connectionString, config);
services.AddScoped<IDeviationScoringService, DeviationScoringService>();

await using var sp = services.BuildServiceProvider();

// ─── Migrate DB ───────────────────────────────────────────────────────────────
using (var scope = sp.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AegisDbContext>();
    await db.Database.MigrateAsync();
    Console.WriteLine("[Aegis.Simulation] Database migrated.");
}

// ─── Seed Data ────────────────────────────────────────────────────────────────
var astronautDefs = new[]
{
    (Name: "Elena Vasquez",    NASAId: "NASA-001", MissionStart: DateTime.UtcNow.AddDays(-90)),
    (Name: "Marcus Chen",      NASAId: "NASA-002", MissionStart: DateTime.UtcNow.AddDays(-75)),
    (Name: "Priya Nair",       NASAId: "NASA-003", MissionStart: DateTime.UtcNow.AddDays(-60)),
    (Name: "James O'Sullivan", NASAId: "NASA-004", MissionStart: DateTime.UtcNow.AddDays(-45)),
    (Name: "Aiko Tanaka",      NASAId: "NASA-005", MissionStart: DateTime.UtcNow.AddDays(-30)),
};

// Realistic baseline ranges per metric
var metricRanges = new Dictionary<MetricType, (double Min, double Max)>
{
    [MetricType.HRV]            = (40,   80),
    [MetricType.SleepQuality]   = (4.0,  9.0),
    [MetricType.BoneDensityIndex] = (0.85, 1.15),
    [MetricType.MoodStressScore]  = (1.0, 10.0),
};

var rng = new Random(42);

foreach (var (name, nasaId, missionStart) in astronautDefs)
{
    using var scope = sp.CreateScope();
    var astronautRepo = scope.ServiceProvider.GetRequiredService<IAstronautRepository>();
    var readingRepo   = scope.ServiceProvider.GetRequiredService<IBiometricReadingRepository>();
    var scoringService = scope.ServiceProvider.GetRequiredService<IDeviationScoringService>();

    // Idempotency check
    var existing = (await astronautRepo.GetAllAsync())
        .FirstOrDefault(a => a.NASAId == nasaId);

    if (existing is not null)
    {
        Console.WriteLine($"[Aegis.Simulation] Astronaut {nasaId} already seeded — skipping.");
        continue;
    }

    var astronaut = new Astronaut
    {
        Id               = Guid.NewGuid(),
        Name             = name,
        NASAId           = nasaId,
        MissionStartDate = missionStart,
    };
    await astronautRepo.AddAsync(astronaut);
    await astronautRepo.SaveChangesAsync();

    int readingCount = 0;

    // 60 days × 4 metrics = 240 readings per astronaut
    for (int day = 60; day >= 1; day--)
    {
        var timestamp = DateTime.UtcNow.AddDays(-day);

        foreach (var (metric, (min, max)) in metricRanges)
        {
            var midpoint = (min + max) / 2.0;
            var noise    = (rng.NextDouble() - 0.5) * (max - min) * 0.15; // ±7.5% noise
            var value    = Math.Clamp(midpoint + noise, min, max);

            var reading = new BiometricReading
            {
                Id           = Guid.NewGuid(),
                AstronautId  = astronaut.Id,
                MetricType   = metric,
                Value        = value,
                RecordedAt   = timestamp,
                ZScore       = 0,
                Severity     = SeverityLevel.None,
            };

            await readingRepo.AddAsync(reading);
            await readingRepo.SaveChangesAsync();

            // Run scoring to accumulate Welford baseline (ignore composite result here)
            await scoringService.ScoreAsync(reading);
            readingCount++;
        }
    }

    Console.WriteLine($"[Aegis.Simulation] Seeded {name} ({nasaId}): {readingCount} readings, baselines computed.");
}

Console.WriteLine("[Aegis.Simulation] Seeding complete.");
