using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using MCPhappey.Common.Extensions;
using MCPhappey.Core.Extensions;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace MCPhappey.Tools.Replicate;

public static class ReplicateService
{
    [Description("Create a prediction using a Replicate model.")]
    [McpServerTool(
        Title = "Create Replicate prediction",
        Name = "replicate_predictions_create",
        Destructive = false)]
    public static async Task<CallToolResult?> ReplicatePredictions_Create(
        [Description("The full model version or owner/model identifier. Example: stability-ai/sdxl")]
        string version,
        [Description("The input payload as JSON object string. Example: { \"prompt\": \"A photo of a cat\" }")]
        string inputJson,
        IServiceProvider serviceProvider,
        RequestContext<CallToolRequestParams> requestContext,
        [Description("Maximum runtime before canceling (e.g. 60s, 5m). Minimum 5 seconds.")]
        string? cancelAfter = null,
        [Description("Wait time (1–60 seconds) before returning intermediate results.")]
        [Range(1, 60)] int? preferWaitSeconds = null,
        CancellationToken cancellationToken = default) =>
        await requestContext.WithExceptionCheck(async () =>
        await requestContext.WithStructuredContent(async () =>
        {
            ArgumentNullException.ThrowIfNullOrWhiteSpace(version);
            ArgumentNullException.ThrowIfNullOrWhiteSpace(inputJson);

            var settings = serviceProvider.GetService<ReplicateSettings>()
                ?? throw new InvalidOperationException("No ReplicateSettings found in service provider.");

            // 1) Elicit user confirmation
            var (typed, notAccepted, _) = await requestContext.Server.TryElicit(
                new ReplicatePredictionRequest
                {
                    Version = version,
                    CancelAfter = cancelAfter,
                    PreferWaitSeconds = preferWaitSeconds
                },
                cancellationToken);

            if (notAccepted != null) throw new Exception();
            if (typed == null) throw new Exception();

            // 2) Build HTTP client
            using var client = new HttpClient { BaseAddress = new Uri("https://api.replicate.com/") };
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);

            if (typed.PreferWaitSeconds is > 0)
                client.DefaultRequestHeaders.Add("Prefer", $"wait={typed.PreferWaitSeconds}");

            if (!string.IsNullOrWhiteSpace(typed.CancelAfter))
                client.DefaultRequestHeaders.Add("Cancel-After", typed.CancelAfter);

            // 3) Build request body
            var body = new
            {
                version = typed.Version,
                input = JsonSerializer.Deserialize<JsonElement>(inputJson),
            };

            var json = JsonSerializer.Serialize(body, new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            using var req = new HttpRequestMessage(HttpMethod.Post, "v1/predictions")
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            // 4) Execute
            using var resp = await client.SendAsync(req, cancellationToken);

            var content = await resp.Content.ReadAsStringAsync(cancellationToken);

            if (!resp.IsSuccessStatusCode)
                throw new Exception($"{resp.ReasonPhrase}: {content}");

            return JsonNode.Parse(content);
        }));

    [Description("Fill in the parameters for Replicate prediction creation.")]
    public class ReplicatePredictionRequest
    {
        [Required]
        [Description("The model version or identifier, e.g. stability-ai/sdxl")]
        [JsonPropertyName("version")]
        public string Version { get; set; } = default!;

        [JsonPropertyName("cancel_after")]
        [Description("Maximum run time (e.g. 1m, 90s).")]
        public string? CancelAfter { get; set; }

        [JsonPropertyName("prefer_wait_seconds")]
        [Range(1, 60)]
        [Description("Wait up to N seconds for model output before returning.")]
        public int? PreferWaitSeconds { get; set; }
    }
}

public class ReplicateSettings
{
    public string ApiKey { get; set; } = default!;
}
