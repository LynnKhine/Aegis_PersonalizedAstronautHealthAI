using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Aegis.Core.Interfaces;
using Aegis.Core.Models;
using Microsoft.Extensions.Options;

namespace Aegis.Infrastructure.Watsonx;

/// <summary>
/// Calls the IBM watsonx.ai text generation REST API to produce a structured
/// intervention plan for an astronaut whose biometric readings have crossed
/// the composite risk threshold.
/// </summary>
public sealed class WatsonxClient : IWatsonxClient
{
    private const string ModelId         = "ibm/granite-3-8b-instruct";
    private const string ApiVersion      = "2023-05-29";
    private const int    MaxNewTokens    = 512;
    private const double Temperature     = 0.3;

    private readonly IHttpClientFactory _httpFactory;
    private readonly WatsonxOptions     _options;

    public WatsonxClient(IHttpClientFactory httpFactory, IOptions<WatsonxOptions> options)
    {
        _httpFactory = httpFactory;
        _options     = options.Value;
    }

    public async Task<InterventionPlanResult> GenerateInterventionPlanAsync(
        Astronaut astronaut,
        IEnumerable<BiometricReading> triggeringReadings,
        CancellationToken ct = default)
    {
        var prompt = BuildPrompt(astronaut, triggeringReadings);

        var requestBody = new
        {
            model_id   = ModelId,
            input      = prompt,
            parameters = new { max_new_tokens = MaxNewTokens, temperature = Temperature },
            project_id = _options.ProjectId
        };

        using var http = _httpFactory.CreateClient("watsonx");
        var url = $"{_options.Endpoint.TrimEnd('/')}/ml/v1/text/generation?version={ApiVersion}";

        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
        request.Content = new StringContent(
            JsonSerializer.Serialize(requestBody),
            Encoding.UTF8,
            "application/json");

        var response = await http.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        var envelope = await response.Content.ReadFromJsonAsync<WatsonxResponse>(
            cancellationToken: ct)
            ?? throw new InvalidOperationException("Empty response from watsonx.ai");

        var generatedText = envelope.Results.FirstOrDefault()?.GeneratedText
            ?? throw new InvalidOperationException("No generated_text in watsonx.ai response");

        return ParseInterventionPlan(generatedText);
    }

    // -------------------------------------------------------------------------
    // Prompt construction
    // -------------------------------------------------------------------------

    private static string BuildPrompt(Astronaut astronaut, IEnumerable<BiometricReading> readings)
    {
        var missionDays = (int)(DateTime.UtcNow - astronaut.MissionStartDate).TotalDays;
        var readingLines = string.Join("\n", readings.Select(r =>
            $"  - {r.MetricType}: value={r.Value:F2}, Z-score={r.ZScore:F2}, severity={r.Severity}"));

        return $$"""
            You are an expert flight surgeon AI assistant for NASA long-duration spaceflight missions.
            An astronaut has triggered a health alert. Analyse the biometric data and respond ONLY
            with a valid JSON object — no markdown, no commentary, no code fences.

            Astronaut: {{astronaut.Name}} (NASA ID: {{astronaut.NASAId}})
            Mission day: {{missionDays}}

            Triggering biometric readings (deviating from personal baseline):
            {{readingLines}}

            Respond with exactly this JSON structure:
            {
              "summary": "<one-paragraph clinical summary of the situation>",
              "immediate_actions": ["<action 1>", "<action 2>", ...],
              "monitoring_frequency": "<e.g. every 2 hours, continuous, daily>",
              "escalate_to_flight_surgeon": <true|false>
            }
            """;
    }

    // -------------------------------------------------------------------------
    // Response parsing
    // -------------------------------------------------------------------------

    private static InterventionPlanResult ParseInterventionPlan(string generatedText)
    {
        // Strip any accidental markdown fences the model may produce
        var json = generatedText.Trim();
        if (json.StartsWith("```"))
        {
            var start = json.IndexOf('{');
            var end   = json.LastIndexOf('}');
            if (start >= 0 && end > start)
                json = json[start..(end + 1)];
        }

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var summary   = root.GetProperty("summary").GetString() ?? string.Empty;
        var frequency = root.GetProperty("monitoring_frequency").GetString() ?? string.Empty;
        var escalate  = root.GetProperty("escalate_to_flight_surgeon").GetBoolean();

        var actions = root.GetProperty("immediate_actions")
            .EnumerateArray()
            .Select(e => e.GetString() ?? string.Empty)
            .ToArray();

        return new InterventionPlanResult(summary, actions, frequency, escalate);
    }

    // -------------------------------------------------------------------------
    // Private DTOs for JSON deserialization
    // -------------------------------------------------------------------------

    private sealed class WatsonxResponse
    {
        [JsonPropertyName("results")]
        public List<WatsonxResult> Results { get; set; } = new();
    }

    private sealed class WatsonxResult
    {
        [JsonPropertyName("generated_text")]
        public string GeneratedText { get; set; } = string.Empty;
    }
}
