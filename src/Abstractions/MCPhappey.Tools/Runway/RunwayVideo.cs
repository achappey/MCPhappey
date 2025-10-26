using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using MCPhappey.Core.Extensions;
using MCPhappey.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using MCPhappey.Common.Extensions;

namespace MCPhappey.Tools.Runway;

public static class RunwayVideo
{
    private const string ApiBase = "https://api.dev.runwayml.com";
    private const string ApiVersion = "2024-11-06";

    // Keep these if you want local guardrails for text-to-video.
    private static readonly HashSet<string> AllowedModels =
    [
        "veo3.1",
        "veo3.1_fast",
        "veo3"
    ];

    private static readonly HashSet<string> AllowedRatios =
    [
        "1280:720",
        "720:1280",
        "1080:1920",
        "1920:1080"
    ];

    private static readonly int[] AllowedDurations = [4, 6, 8];

    // ---------- TEXT → VIDEO ----------

    [Description("Start a Runway text-to-video task. Returns the task id.")]
    [McpServerTool(
        Title = "Create Runway Text-to-Video",
        Name = "runway_text_to_video",
        OpenWorld = true,
        ReadOnly = false,
        Destructive = false)]
    public static async Task<CallToolResult?> Runway_TextToVideo(
        [Description("Prompt describing the video, max 1000 UTF-16 code units.")]
        string? promptText,
        [Description("Model variant: veo3.1 | veo3.1_fast | veo3. Default = veo3.1")]
        string? model,
        [Description("Aspect ratio: 1280:720 | 720:1280 | 1080:1920 | 1920:1080. Default = 1280:720")]
        string? ratio,
        [Description("Duration in seconds. For 'veo3' it must be 8. For 'veo3.1' and 'veo3.1_fast': 4 | 6 | 8. Default = 6 or 8 depending on model.")]
        int? duration,
        [Description("Optional seed 0..4294967295 for reproducibility.")]
        int? seed,
        IServiceProvider serviceProvider,
        RequestContext<CallToolRequestParams> requestContext,
        CancellationToken cancellationToken = default)
        => await requestContext.WithExceptionCheck(async () =>
           await requestContext.WithStructuredContent(async () =>
        {
            // Elicit a typed request first
            var (typed, notAccepted, _) = await requestContext.Server.TryElicit(new RunwayNewVideoRequest
            {
                PromptText = promptText ?? "",
                Model = string.IsNullOrWhiteSpace(model) ? "veo3.1" : model!,
                Ratio = string.IsNullOrWhiteSpace(ratio) ? "1280:720" : ratio!,
                Duration = duration,
                Seed = seed
            }, cancellationToken);

            if (notAccepted != null) throw new Exception(JsonSerializer.Serialize(notAccepted));
            if (typed == null) throw new Exception("Invalid input.");

            // Defaults after elicitation
            if (!typed.Duration.HasValue)
                typed.Duration = typed.Model == "veo3" ? 8 : 6;

            Validate(typed);

            var settings = serviceProvider.GetRequiredService<RunwaySettings>();
            var clientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();

            var jsonBody = JsonSerializer.Serialize(new
            {
                promptText = typed.PromptText,
                model = typed.Model,
                ratio = typed.Ratio,
                duration = typed.Duration,
                seed = typed.Seed
            });

            using var client = clientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);
            client.DefaultRequestHeaders.Add("X-Runway-Version", ApiVersion);

            using var req = new HttpRequestMessage(HttpMethod.Post, $"{ApiBase}/v1/text_to_video")
            {
                Content = new StringContent(jsonBody, Encoding.UTF8, "application/json")
            };

            using var resp = await client.SendAsync(req, cancellationToken);
            var json = await resp.Content.ReadAsStringAsync(cancellationToken);

            if (!resp.IsSuccessStatusCode)
                throw new Exception($"{resp.StatusCode}: {json}");

            return await JsonNode.ParseAsync(BinaryData.FromString(json).ToStream(), cancellationToken: cancellationToken);
        }));

    // ---------- IMAGE → VIDEO ----------

    [Description("Start an image-to-video task on Runway. Returns the task id.")]
    [McpServerTool(
        Title = "Create Runway Image-to-Video",
        Name = "runway_image_to_video_create",
        OpenWorld = true,
        ReadOnly = false,
        Destructive = false)]
    public static async Task<CallToolResult?> Runway_ImageToVideo_Create(
        [Description("List of image URLs to guide the generation. For a single image, it will be used as the first frame by default.")]
        IEnumerable<string> promptImages,
        [Description("Optional text prompt describing desired appearance.")]
        string? promptText,
        [Description("Model variant string. See Runway docs for valid values.")]
        string? model,
        [Description("Aspect ratio string. See Runway docs for valid values.")]
        string? ratio,
        [Description("Duration (seconds). 2–10 depending on model. For example, veo3 requires 8; veo3.1/veo3.1_fast allow 4, 6, or 8.")]
        int? duration,
        [Description("Optional seed in [0..4294967295].")]
        int? seed,
        IServiceProvider serviceProvider,
        RequestContext<CallToolRequestParams> requestContext,
        CancellationToken cancellationToken = default)
        => await requestContext.WithExceptionCheck(async () =>
           await requestContext.WithStructuredContent(async () =>
        {
            if (promptImages == null || !promptImages.Any())
                throw new ValidationException("At least one promptImage URL is required.");

            // Build a typed elicitation object with defaults
            var (typed, notAccepted, _) = await requestContext.Server.TryElicit(new RunwayNewImageToVideo
            {
                // Seed with provided values; user can fill missing via elicitation
                PromptText = promptText,
                Model = string.IsNullOrWhiteSpace(model) ? "veo3.1" : model!,
                Ratio = string.IsNullOrWhiteSpace(ratio) ? "1280:720" : ratio!,
                Duration = duration ?? 6,
                Seed = seed
            }, cancellationToken);

            if (notAccepted != null) throw new Exception(JsonSerializer.Serialize(notAccepted));
            if (typed == null) throw new Exception("Invalid input.");

            if (promptImages == null || promptImages.Count() == 0)
                throw new ValidationException("At least one promptImage is required.");

            // NOTE: Do not hardcode model/ratio lists here; rely on API docs and surface guidance in errors.
            if (typed.Duration < 2 || typed.Duration > 10)
                throw new ValidationException("Duration must be between 2 and 10 seconds depending on model. See Runway docs.");

            var downloadService = serviceProvider.GetRequiredService<DownloadService>();
            var settings = serviceProvider.GetRequiredService<RunwaySettings>();
            var clientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();

            // Convert images to data URIs (base64) to satisfy the "uri" contract as data URIs
            var promptImagePayload = new List<object>();
            string? position = null;

            foreach (var img in promptImages)
            {
                if (string.IsNullOrWhiteSpace(img))
                    throw new ValidationException("Each image must have a non-empty uri.");

                // Download and turn into base64 data URI
                var files = await downloadService.DownloadContentAsync(serviceProvider, requestContext.Server, img, cancellationToken);
                var bytes = files.FirstOrDefault()?.Contents;
                if (bytes == null) throw new Exception($"Failed to download image: {img}");

                string base64 = Convert.ToBase64String(bytes.ToArray());
                // Default to image/png if content type is unknown
                string dataUri = $"data:image/png;base64,{base64}";

                promptImagePayload.Add(new
                {
                    uri = dataUri,
                    position = position == null ? "first" : "last"
                });
                position = "first";
            }

            var jsonBody = JsonSerializer.Serialize(new
            {
                promptImage = promptImagePayload.Count == 1 ? promptImagePayload[0] : promptImagePayload,
                seed = typed.Seed,
                model = typed.Model,
                promptText = typed.PromptText,
                duration = typed.Duration,
                ratio = typed.Ratio
                // contentModeration optional and model-dependent; omit by default
            });

            using var client = clientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);
            client.DefaultRequestHeaders.Add("X-Runway-Version", ApiVersion);

            using var req = new HttpRequestMessage(HttpMethod.Post, $"{ApiBase}/v1/image_to_video")
            {
                Content = new StringContent(jsonBody, Encoding.UTF8, "application/json")
            };

            using var resp = await client.SendAsync(req, cancellationToken);
            var json = await resp.Content.ReadAsStringAsync(cancellationToken);

            if (!resp.IsSuccessStatusCode)
                throw new Exception($"{resp.StatusCode}: {json}");

            // Expected: { "id": "..." }
            return await JsonNode.ParseAsync(BinaryData.FromString(json).ToStream(), cancellationToken: cancellationToken);
        }));

    private static void Validate(RunwayNewVideoRequest input)
    {
        if (string.IsNullOrWhiteSpace(input.PromptText))
            throw new ValidationException("promptText is required.");
        if (input.PromptText.Length > 1000)
            throw new ValidationException("promptText must be at most 1000 characters.");

        if (string.IsNullOrWhiteSpace(input.Model) || !AllowedModels.Contains(input.Model))
            throw new ValidationException($"model must be one of [{string.Join(", ", AllowedModels)}].");

        if (string.IsNullOrWhiteSpace(input.Ratio) || !AllowedRatios.Contains(input.Ratio))
            throw new ValidationException($"ratio must be one of [{string.Join(", ", AllowedRatios)}].");

        if (!input.Duration.HasValue)
            throw new ValidationException("duration is required.");

        if (!AllowedDurations.Contains(input.Duration.Value))
            throw new ValidationException("duration must be 4, 6, or 8.");

        if (input.Model == "veo3" && input.Duration != 8)
            throw new ValidationException("for model 'veo3' the duration must be 8.");

        if (input.Seed is < 0 or > int.MaxValue)
            throw new ValidationException("seed must be between 0 and 4294967295.");
    }

    // -------- DTOs for elicitation --------

    [Description("Typed input for Runway text-to-video creation.")]
    public class RunwayNewVideoRequest
    {
        [JsonPropertyName("promptText")]
        [Required]
        [Description("Prompt describing the video to generate. Non-empty; up to 1000 UTF-16 code units.")]
        public string PromptText { get; set; } = default!;

        [JsonPropertyName("model")]
        [Required]
        [Description("Model variant. One of: veo3.1 | veo3.1_fast | veo3.")]
        public string Model { get; set; } = default!;

        [JsonPropertyName("ratio")]
        [Required]
        [Description("Aspect ratio string. One of: 1280:720 | 720:1280 | 1080:1920 | 1920:1080.")]
        public string Ratio { get; set; } = default!;

        [JsonPropertyName("duration")]
        [Description("Video duration in seconds (4, 6, or 8). For 'veo3' the duration must be 8.")]
        public int? Duration { get; set; }

        [JsonPropertyName("seed")]
        [Description("Optional seed for reproducibility. Integer in the inclusive range [0, 4294967295].")]
        [Range(0, int.MaxValue)]
        public int? Seed { get; set; }
    }

    [Description("Typed input for Runway image-to-video creation.")]
    public class RunwayNewImageToVideo
    {
        [JsonPropertyName("promptText")]
        [Description("Optional text prompt to describe the desired content.")]
        public string? PromptText { get; set; }

        [JsonPropertyName("model")]
        [Required]
        [Description("Model variant. See Runway docs for valid values (e.g., gen4_turbo, gen3a_turbo, veo3.1, veo3.1_fast, veo3).")]
        public string Model { get; set; } = default!;

        [JsonPropertyName("ratio")]
        [Required]
        [Description("Output aspect ratio string (e.g., 1280:720, 720:1280, 1080:1920, 1920:1080).")]
        public string Ratio { get; set; } = default!;

        [JsonPropertyName("duration")]
        [Required]
        [Description("Video duration in seconds. Typically 2–10 depending on model. For example, veo3 requires 8; veo3.1/veo3.1_fast allow 4, 6, or 8.")]
        public int Duration { get; set; }

        [JsonPropertyName("seed")]
        [Description("Optional seed in [0..4294967295] for reproducibility.")]
        [Range(0, int.MaxValue)]
        public int? Seed { get; set; }
    }

}


public class RunwaySettings
{
    public string ApiKey { get; set; } = default!;
}