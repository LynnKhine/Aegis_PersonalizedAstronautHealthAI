# Aegis — Personalized Astronaut Health AI: Implementation Plan

## Top-Level Overview

Build a .NET 8 solution called **Aegis** that ingests astronaut biometric readings, scores each one against that astronaut's personal baseline using a Z-score + tiered severity model, computes a composite risk score across all active metrics, and — when the composite score crosses a threshold — calls IBM watsonx.ai to generate a structured intervention plan that is pushed in real time to the astronaut's SignalR group.

The solution contains four projects built in strict dependency order:

```
Aegis.Core (zero deps)
    ↑
Aegis.Infrastructure (depends on Core)
    ↑
Aegis.Simulation (depends on Core + Infrastructure)
    ↑
Aegis.Api (depends on all three)
```

**Acceptance criterion:** POSTing a new biometric reading for an astronaut triggers deviation scoring against their personal baseline; if the composite risk score ≥ 2, an AI-generated intervention plan is created and pushed in real time via SignalR to that astronaut's group only.

---

## Domain Model Reference

### Entities

| Entity | Key Fields |
|---|---|
| `Astronaut` | `Id` (Guid), `Name`, `NASAId`, `MissionStartDate` |
| `BiometricReading` | `Id`, `AstronautId`, `MetricType`, `Value`, `RecordedAt`, `ZScore`, `Severity` |
| `PersonalBaseline` | `Id`, `AstronautId`, `MetricType`, `Mean`, `StdDev`, `SampleCount`, `LastUpdated` |
| `InterventionPlan` | `Id`, `AstronautId`, `TriggeredByReadingId`, `Summary`, `ImmediateActionsJson`, `MonitoringFrequency`, `EscalateToFlightSurgeon`, `GeneratedAt` |

### Enums

- `MetricType`: `HRV`, `SleepQuality`, `BoneDensityIndex`, `MoodStressScore`
- `SeverityLevel`: `None`, `Warning`, `Alert`, `Critical`

### Severity ↔ Z-score mapping

| Band | Z-score range | Tier weight |
|---|---|---|
| None | < 1.5σ | 0 |
| Warning | 1.5 – 2σ | 1 |
| Alert | 2 – 3σ | 2 |
| Critical | > 3σ | 3 |

**Escalation gate:** composite risk score = sum of tier weights across the astronaut's most-recent reading per metric. If composite score ≥ 2, escalate to watsonx.

### Baseline update algorithm

Use **Welford's online algorithm** — updates running mean and variance incrementally from a single new reading without re-scanning history. Fields stored: `Mean`, `StdDev`, `SampleCount`. This is the correct numerical-methods choice for a streaming health monitor and should be highlighted in the README architecture section.

---

## Sub-Tasks

---

### Sub-Task 1 — Scaffold the .NET solution and four projects

**Intent:** Create the solution structure on disk so all subsequent tasks have a stable home. No application logic — purely project files, folder layout, and cross-project references.

**Expected Outcomes:**
- `Aegis.sln` updated with four `.csproj` references
- Folder layout: `src/Aegis.Core`, `src/Aegis.Infrastructure`, `src/Aegis.Simulation`, `src/Aegis.Api`
- Each project targets `net8.0`
- Project references wired: Infrastructure → Core; Simulation → Core + Infrastructure; Api → Core + Infrastructure
- Solution builds with `dotnet build` (empty projects, no errors)

**Todo List:**
1. Run `dotnet new classlib -n Aegis.Core -o src/Aegis.Core --framework net8.0`
2. Run `dotnet new classlib -n Aegis.Infrastructure -o src/Aegis.Infrastructure --framework net8.0`
3. Run `dotnet new console -n Aegis.Simulation -o src/Aegis.Simulation --framework net8.0`
4. Run `dotnet new webapi -n Aegis.Api -o src/Aegis.Api --framework net8.0`
5. Add all four projects to `Aegis.sln` via `dotnet sln add`
6. Add cross-project references (`dotnet add reference`) per the dependency chain above
7. Delete the default `Class1.cs` stubs; verify `dotnet build Aegis.sln` succeeds

**Relevant Context:** `Aegis.sln` already exists at repo root — add to it, do not replace it.

**Status:** `[x] done`

---

### Sub-Task 2 — Define domain models, enums, and interfaces in Aegis.Core

**Intent:** Establish the pure domain layer with zero external dependencies. All other projects depend on these types, so they must be stable before any other code is written.

**Expected Outcomes:**
- `Aegis.Core/Models/` contains `Astronaut.cs`, `BiometricReading.cs`, `PersonalBaseline.cs`, `InterventionPlan.cs`
- `Aegis.Core/Enums/` contains `MetricType.cs` and `SeverityLevel.cs`
- `Aegis.Core/Interfaces/` contains repository contracts (`IAstronautRepository`, `IBiometricReadingRepository`, `IPersonalBaselineRepository`, `IInterventionPlanRepository`) and service contracts (`IDeviationScoringService`, `IWatsonxClient`)
- No NuGet packages added to Core
- All types in namespace `Aegis.Core`

**Todo List:**
1. Create `Aegis.Core/Enums/MetricType.cs` with the four metric values
2. Create `Aegis.Core/Enums/SeverityLevel.cs` with `None`, `Warning`, `Alert`, `Critical`
3. Create the four model classes with the fields listed in the Domain Model Reference above
4. Add a `CompositeRiskScore` value-type or record in Core to carry `(int Score, List<BiometricReading> Contributors)` — used by the scoring service
5. Create repository interfaces (CRUD + query-by-astronaut methods needed by the service)
6. Create `IDeviationScoringService` with method `ScoreAsync(BiometricReading reading) : Task<CompositeRiskScore>`
7. Create `IWatsonxClient` with method `GenerateInterventionPlanAsync(Astronaut astronaut, IEnumerable<BiometricReading> triggeringReadings) : Task<InterventionPlanResult>`
8. Create `InterventionPlanResult` record in Core: `Summary`, `ImmediateActions` (string[]), `MonitoringFrequency`, `EscalateToFlightSurgeon`

**Relevant Context:** No EF Core, no HTTP clients — pure C# records/classes only.

**Status:** `[x] done`

---

### Sub-Task 3 — Implement deviation scoring in Aegis.Core

**Intent:** The `DeviationScoringService` is pure logic (it calls repository interfaces, not concrete classes), making it fully unit-testable. It computes the Z-score for the new reading, maps it to a severity tier, updates the astronaut's baseline via Welford's algorithm, then sums tier weights across all metrics to produce a composite risk score.

**Expected Outcomes:**
- `Aegis.Core/Services/DeviationScoringService.cs` implements `IDeviationScoringService`
- Z-score computed as `(value − mean) / stdDev` (guard: if `SampleCount < 2` or `stdDev == 0`, severity = `None`)
- Severity mapped to tier weight: `None=0`, `Warning=1`, `Alert=2`, `Critical=3`
- Welford update applied to the `PersonalBaseline` and persisted via `IPersonalBaselineRepository`
- Composite score = sum of the most-recent tier weight per metric for that astronaut
- Returns `CompositeRiskScore` with the score value and the contributing readings

**Todo List:**
1. Create `Aegis.Core/Services/DeviationScoringService.cs`
2. Inject `IPersonalBaselineRepository` and `IBiometricReadingRepository` via constructor
3. Implement Z-score computation with the cold-start guard
4. Implement severity band mapping
5. Implement Welford's incremental mean/variance update on `PersonalBaseline`
6. Persist the updated baseline via the repository interface
7. Fetch the most-recent reading per metric for the astronaut (excluding the new reading)
8. Sum tier weights (including new reading's weight) to produce composite score
9. Return `CompositeRiskScore`

**Relevant Context:** Welford's algorithm: `newMean = mean + (value − mean) / n`; `newM2 = M2 + (value − mean) * (value − newMean)`; `stdDev = sqrt(M2 / (n − 1))`. Store M2 as `Variance * (SampleCount − 1)` and recompute each update — or store `SumOfSquaredDeviations` directly as an extra column.

**Status:** `[x] done`

---

### Sub-Task 4 — Implement EF Core DbContext and repositories in Aegis.Infrastructure

**Intent:** Wire up SQLite persistence via EF Core code-first, implement the four repository interfaces, and expose the `DbContext` for DI registration. No business logic here.

**Expected Outcomes:**
- NuGet packages added: `Microsoft.EntityFrameworkCore.Sqlite`, `Microsoft.EntityFrameworkCore.Design`
- `AegisDbContext.cs` with `DbSet` for all four entities, fluent configuration (indexes on `AstronautId` + `MetricType`, `RecordedAt`)
- Four concrete repository classes implementing Core interfaces
- An EF Core initial migration generated and included in source
- `IServiceCollection` extension method `AddAegisInfrastructure(connectionString)` for clean DI wiring in Api and Simulation

**Todo List:**
1. Add NuGet packages to `Aegis.Infrastructure.csproj`
2. Create `Aegis.Infrastructure/Data/AegisDbContext.cs`
3. Configure entity mappings (table names, PKs, FKs, indexes) using `OnModelCreating`
4. Create `Aegis.Infrastructure/Repositories/` with `AstronautRepository`, `BiometricReadingRepository`, `PersonalBaselineRepository`, `InterventionPlanRepository`
5. Each repository implements the corresponding Core interface using the injected `AegisDbContext`
6. Add `Aegis.Infrastructure/Extensions/ServiceCollectionExtensions.cs` with `AddAegisInfrastructure`
7. Run `dotnet ef migrations add InitialCreate` from the Infrastructure project context (using the Api as the startup project)
8. Verify migration compiles cleanly

**Relevant Context:** The `PersonalBaseline` entity needs a `SumOfSquaredDeviations` column to support Welford's algorithm (see Sub-Task 3). Add this field to the model in Core and the migration here.

**Status:** `[x] done`

---

### Sub-Task 5 — Implement the WatsonxClient in Aegis.Infrastructure

**Intent:** Encapsulate all IBM watsonx.ai HTTP communication in one class. Constructs the prompt, calls the `ibm/granite-3-8b-instruct` model, and deserializes the structured JSON response into `InterventionPlanResult`.

**Expected Outcomes:**
- `Aegis.Infrastructure/Watsonx/WatsonxClient.cs` implements `IWatsonxClient`
- Reads `WatsonxOptions` (ApiKey, ProjectId, Endpoint) from configuration
- Constructs a prompt that includes: astronaut name, NASAId, mission duration, the triggering readings with their metric/value/severity, and an explicit instruction to respond in JSON with fields `summary`, `immediate_actions` (array), `monitoring_frequency`, `escalate_to_flight_surgeon` (bool)
- Calls `POST /ml/v1/text/generation?version=2023-05-29` on the watsonx.ai endpoint
- Deserializes the `generated_text` field from the watsonx response envelope
- Parses `generated_text` as JSON into `InterventionPlanResult`
- Registered in `AddAegisInfrastructure` via `IHttpClientFactory`

**Todo List:**
1. Add `WatsonxOptions.cs` record to Infrastructure (ApiKey, ProjectId, Endpoint URL)
2. Create `WatsonxClient.cs` with `IHttpClientFactory` injection
3. Implement `GenerateInterventionPlanAsync`: build request body, POST, handle non-2xx, deserialize
4. Write prompt template as a private const/method — keep it readable and structured
5. Register `WatsonxOptions` binding and `WatsonxClient` as `IWatsonxClient` in `AddAegisInfrastructure`
6. Use `System.Text.Json` for all serialization (no Newtonsoft)

**Relevant Context:** watsonx.ai REST API request body shape:
```json
{
  "model_id": "ibm/granite-3-8b-instruct",
  "input": "<prompt>",
  "parameters": { "max_new_tokens": 512, "temperature": 0.3 },
  "project_id": "<ProjectId>"
}
```
Response envelope: `{ "results": [ { "generated_text": "..." } ] }`. The model is `ibm/granite-3-8b-instruct` — verify the exact ID in the watsonx.ai model picker before demo day; IBM iterates this.

**Status:** `[x] done`

---

### Sub-Task 6 — Build the Aegis.Simulation console seeder

**Intent:** Pre-populate SQLite with realistic astronaut profiles and 60 days of historical biometric readings so that personal baselines are already computed when the demo starts. Without seeded history, the cold-start guard in Sub-Task 3 would suppress all escalations.

**Expected Outcomes:**
- `Aegis.Simulation/Program.cs` is a standalone console app
- Seeds 3–5 astronaut profiles with NASAId and mission start dates
- For each astronaut, generates 60 days × 4 metrics = 240 readings with realistic value ranges and small random noise
- Calls `DeviationScoringService` for each historical reading to build up the `PersonalBaseline` rows via Welford updates
- Idempotent: checks if astronauts already exist before inserting
- Prints a summary to stdout on completion
- Run via `dotnet run --project src/Aegis.Simulation`

**Todo List:**
1. Add `appsettings.json` to Simulation with SQLite connection string
2. Wire DI: `AddAegisInfrastructure(connectionString)` + `DeviationScoringService` registration
3. Call `dbContext.Database.MigrateAsync()` on startup to ensure schema exists
4. Define realistic value ranges per metric (e.g. HRV: 40–80 ms, SleepQuality: 4–9, BoneDensityIndex: 0.85–1.15, MoodStressScore: 1–10)
5. Seed astronaut rows with idempotency check
6. Loop: for each astronaut, for each of 60 days, for each metric — generate a reading and call `DeviationScoringService.ScoreAsync` (ignore composite result; only the baseline side-effect matters)
7. Print count of readings seeded per astronaut

**Relevant Context:** The simulation must use `DeviationScoringService` (not direct SQL) to ensure the Welford baseline accumulation path is exercised exactly as it will be at runtime.

**Status:** `[x] done`

---

### Sub-Task 7 — Build the Aegis.Api: controllers, SignalR hub, and DI wiring

**Intent:** Expose the three API endpoints, host the SignalR hub, and wire together the full request pipeline: ingest → score → (if composite ≥ 2) call watsonx → save plan → push to astronaut's SignalR group.

**Expected Outcomes:**
- `POST /api/readings` — accepts `IngestReadingRequest` (AstronautId, MetricType, Value, RecordedAt), persists reading, runs scoring, conditionally triggers watsonx and saves the plan, returns `201` with `ReadingResponse` (reading id, severity, composite score, intervention plan if generated)
- `GET /api/astronauts/{id}/status` — returns the astronaut's current composite risk score plus their most-recent reading per metric
- `GET /api/astronauts/{id}/readings` — returns paginated reading history (default page size 50, supports `?metric=` filter and `?page=` param)
- `AegisHub` (SignalR) at `/hubs/aegis` — clients join by calling `JoinAstronautGroup(astronautId)`; server sends `InterventionPlanGenerated` message to group `astronaut-{astronautId}`
- `Program.cs` registers all services via `AddAegisInfrastructure` and maps routes + hub
- `appsettings.json` contains SQLite connection string and `WatsonxOptions` section

**Todo List:**
1. Add NuGet: `Microsoft.AspNetCore.SignalR` (included in ASP.NET Core 8, no extra package needed)
2. Create `Aegis.Api/Hubs/AegisHub.cs` with `JoinAstronautGroup` method and a typed client interface `IAegisClient` with `InterventionPlanGenerated(InterventionPlan plan)`
3. Create `Aegis.Api/Controllers/ReadingsController.cs` with `POST /api/readings`
4. In the POST handler: save reading → call `DeviationScoringService.ScoreAsync` → if composite ≥ 2, call `IWatsonxClient.GenerateInterventionPlanAsync` → map result to `InterventionPlan` entity → save via repository → push to SignalR group via `IHubContext<AegisHub, IAegisClient>`
5. Create `Aegis.Api/Controllers/AstronautsController.cs` with `GET /status` and `GET /readings`
6. Create request/response DTOs in `Aegis.Api/Models/`
7. Wire `Program.cs`: `AddAegisInfrastructure`, `AddScoped<IDeviationScoringService, DeviationScoringService>`, `AddSignalR`, `MapHub<AegisHub>("/hubs/aegis")`, `MapControllers`
8. Add `appsettings.json` with connection string and `WatsonxOptions` placeholder
9. Verify `dotnet build Aegis.sln` is clean
10. Manual smoke test: run Simulation seeder, then POST a reading via curl/Swagger and confirm response shape

**Relevant Context:** Use `IHubContext<AegisHub, IAegisClient>` (not `IHubContext<AegisHub>`) so the push call is strongly typed. Group name convention: `"astronaut-" + astronautId.ToString()`.

**Status:** `[x] done`

---

## Architecture Notes for README

The following points are worth highlighting in the project README's architecture section:

1. **Welford's online algorithm** — incremental mean/variance without storing full history; the numerically stable choice for a streaming health monitor
2. **Composite risk scoring** — single-metric anomalies are noise; the composite gate (sum of tier weights ≥ 2) requires meaningful multi-metric deviation before escalating to the AI, reducing false positives
3. **Per-astronaut SignalR groups** — isolation by design; each astronaut dashboard only receives its own alerts
4. **Cold-start guard** — Z-score suppressed until `SampleCount ≥ 2` and `StdDev > 0`; the simulation seeder exists specifically to prime this with 60 days of history
5. **Model note** — targeting `ibm/granite-3-8b-instruct`; verify exact model ID in the watsonx.ai project model picker before demo, as IBM has iterated the Granite 3.x line
