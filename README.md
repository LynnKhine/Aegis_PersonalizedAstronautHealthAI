# Aegis — Personalized Astronaut Health AI

**An AI health companion that catches astronaut health decline before it becomes a mission risk.**

Built for the **BeMyApp x IBM AI Builders Challenge For IBM August 2026 Hackathon Event — Advance Space Exploration with AI**.

---

## Problem Statement

Long-duration spaceflight quietly erodes the human body. In microgravity, astronauts lose roughly 1–2% of bone density per month, alongside muscle atrophy, cardiovascular deconditioning, disrupted sleep, and psychological strain from prolonged isolation.

Today, these risks are tracked reactively and manually — health data is reviewed periodically by ground teams, not continuously monitored per individual. By the time a decline is flagged, it has often already progressed. As commercial spaceflight expands and mission durations grow, this reactive, one-size-fits-all approach won't scale to the number of people who will eventually live and work in space.

## Solution Description

**Aegis** is a personalized health AI for astronauts on long-duration missions. Instead of monitoring against generic fleet-wide thresholds, Aegis learns each astronaut's *individual* biometric baseline — heart rate variability, sleep quality, bone density trend, and self-reported mood/stress — and continuously scores how far current readings deviate from that person's own normal.

When a meaningful deviation is detected, an AI layer interprets the deviation and generates a concrete, actionable intervention — a specific exercise session, a rest adjustment, or a psychological check-in prompt — along with a plain-language explanation of why it matters. The recommendation is pushed to the astronaut's schedule in real time, so the intervention arrives before the underlying problem compounds, rather than days later after a ground-based review.

Aegis treats each astronaut as an individual, not a data point in an aggregate — a preview of how personalized AI health support could work for anyone, built first for the highest-stakes environment we can imagine.

## Challenge Theme & Solution Area

**Theme:** Advance Space Exploration with AI
**Solution area:** Space Safety & Predictive Monitoring — analyzing biometric/health telemetry to predict decline and support proactive, individualized decision-making rather than reactive review.

## AI Approach and Architecture

**Pipeline:**
```
Biometric reading ingested
        ↓
Deviation-scoring service (compares reading against astronaut's personal baseline)
        ↓
Threshold crossed? → AI layer (IBM watsonx / Granite) generates a structured
intervention plan from the deviation report
        ↓
SignalR hub pushes the intervention plan in real time to the astronaut's
connected dashboard/schedule
```

**Design principles:**
- **AI interprets, deterministic logic decides when to escalate.** Deviation thresholds are computed with transparent, explainable rules; the AI's job is to turn a flagged deviation into a specific, useful recommendation — not to decide silently in the background whether something is "wrong."
- **Structured AI output, not raw text.** The watsonx/Granite call returns a strongly-typed intervention object (summary, countermeasure, urgency) so it can be reliably rendered and pushed, not just displayed as a chat message.
- **Individual baselines, not population averages.** This is the core differentiator from typical fleet-wide anomaly monitoring — each astronaut is scored against their own trend, not a generic norm.

**Components:**
- **Data model:** Astronaut profile, biometric readings (HRV, sleep, bone density index, mood/stress), personal baseline, intervention plans.
- **Ingestion API:** Accepts new biometric readings per astronaut.
- **Deviation-scoring service:** Compares incoming readings against the rolling personal baseline; flags significant deviations.
- **AI service:** Calls IBM watsonx/Granite with a structured deviation report and returns a structured intervention plan.
- **Real-time layer:** SignalR hub pushes intervention plans to connected clients, grouped by astronaut ID.
- **Simulation seeder:** Generates realistic biometric drift over time for multiple astronaut profiles, based on ranges from published NASA bedrest/ISS studies, since real astronaut biometric data isn't publicly accessible.

## Tech Stack

| Layer | Technology |
|---|---|
| Backend | .NET Core Web API |
| Real-time delivery | SignalR |
| Data access | Entity Framework Core |
| AI | IBM watsonx / Granite |
| Database | [SQL Server / PostgreSQL — update once finalized] |
| Development environment | IBM Bob |

## How IBM Bob Was Used

- **Plan mode** to scope the data model, API layers, and where the AI call fits in the pipeline before any code was written.
- **Agent mode** to implement the data model/repository layer, the biometric ingestion endpoint, the deviation-scoring service, the watsonx integration service, and the SignalR hub — built incrementally, layer by layer, in separate sessions.
- **Context mentions (`@file`)** to keep Bob working against actual existing code rather than re-pasting files into chat.
- Bob-assisted debugging and iteration throughout implementation, with checkpoints used to safely roll back experimental changes.
- README drafting support to keep documentation in sync with the implemented architecture.

## Data Sources / Simulation Note

Real astronaut biometric telemetry is not publicly available. Aegis uses a simulated data seeder that generates biometric drift over a simulated mission timeline (e.g., bone density decline, sleep disruption trends) using realistic ranges drawn from published NASA bedrest and ISS research, rather than random noise — so the demo reflects plausible real-world decline patterns.

## Getting Started

### Prerequisites
- .NET SDK 8+
- [Database — SQL Server / PostgreSQL instance]
- IBM watsonx API key and project ID

### Installation

```bash
git clone https://github.com/<your-username>/aegis-astronaut-ai.git
cd aegis-astronaut-ai
dotnet restore
```

### Environment Variables

Create an `appsettings.Development.json` (or `.env`, depending on setup):

```
WATSONX_API_KEY=...
WATSONX_PROJECT_ID=...
WATSONX_MODEL_ID=...
CONNECTION_STRING=...
```

### Run Locally

```bash
dotnet run --project src/Aegis.Api
```

## Project Structure

```
aegis-astronaut-ai/
├── src/
│   ├── Aegis.Api/            # Web API entry point, controllers, SignalR hub
│   ├── Aegis.Core/           # Domain models, interfaces
│   ├── Aegis.Infrastructure/ # EF Core, repositories, watsonx client
│   └── Aegis.Simulation/     # Biometric drift seeder/simulator
├── README.md
└── Aegis.sln
```

## Demo Video

▶️ [Watch the demo (≤ 3 minutes)](#) — link to be added

## Team

[Your name / team members]

## License

MIT
