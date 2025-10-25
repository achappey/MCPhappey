using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using MCPhappey.Common.Extensions;
using MCPhappey.Core.Extensions;
using MCPhappey.Tools.Together.Images;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace MCPhappey.Tools.Together.Video;

public static class TogetherVideo
{
    [Description("List all Together AI video models.")]
    [McpServerTool(Title = "List Together AI Video Models",
        Name = "together_video_list_models",
        ReadOnly = true)]
    public static async Task<CallToolResult?> TogetherVideo_ListModels(
        IServiceProvider serviceProvider,
        RequestContext<CallToolRequestParams> requestContext,
        CancellationToken cancellationToken = default)
        => await requestContext.WithExceptionCheck(async () =>
            await requestContext.WithStructuredContent(async () =>
        {
            var settings = serviceProvider.GetRequiredService<TogetherSettings>();
            var clientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();

            using var client = clientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);

            using var resp = await client.GetAsync("https://api.together.xyz/v1/models", cancellationToken);
            var json = await resp.Content.ReadAsStringAsync(cancellationToken);
            if (!resp.IsSuccessStatusCode)
                throw new Exception($"{resp.StatusCode}: {json}");

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var models = (root.ValueKind == JsonValueKind.Array
                ? root.EnumerateArray()
                : root.GetProperty("data").EnumerateArray())
                .Where(e => e.TryGetProperty("type", out var t) && t.GetString() == "video")
                .Select(e => JsonNode.Parse(e.GetRawText())!)
                .ToArray();

            return new JsonObject { ["models"] = new JsonArray(models) };
        }));

    [Description("Create a new video using Together AI video models.")]
    [McpServerTool(Title = "Create Together AI Video", Name = "together_video_create",
        OpenWorld = true,
        ReadOnly = false,
        Destructive = false)]
    public static async Task<CallToolResult?> TogetherVideo_Create(
        [Description("Prompt describing the video to generate.")] string prompt,
        [Description("Video model to use, e.g. black-forest-labs/FLUX.1-schnell-video")] string model,
        IServiceProvider serviceProvider,
        RequestContext<CallToolRequestParams> requestContext,
        [Description("Video duration in seconds.")] int seconds = 6,
        [Description("Frames per second (default 24).")] int fps = 24,
        [Description("Width of the video.")] int width = 1024,
        [Description("Height of the video.")] int height = 1024,
        [Description("Optional negative prompt.")] string? negativePrompt = null,
        [Description("Optional seed for deterministic generation.")] int? seed = null,
        [Description("Filename (without extension).")] string? filename = null,
        CancellationToken cancellationToken = default)
        => await requestContext.WithExceptionCheck(async () =>
            await requestContext.WithStructuredContent(async () =>
        {
            ArgumentNullException.ThrowIfNullOrWhiteSpace(prompt);
            ArgumentNullException.ThrowIfNullOrWhiteSpace(model);

            var settings = serviceProvider.GetRequiredService<TogetherSettings>();
            var clientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
            filename ??= requestContext.ToOutputFileName("mp4");

            var (typed, notAccepted, result) = await requestContext.Server.TryElicit(new TogetherNewVideo
            {
                Prompt = prompt,
                Model = model,
                Seconds = seconds,
                Fps = fps,
                Width = width,
                Height = height,
                NegativePrompt = negativePrompt,
                Seed = seed,
                Filename = filename
            }, cancellationToken);

            if (notAccepted != null) throw new Exception(JsonSerializer.Serialize(notAccepted));
            if (typed == null) throw new Exception("Invalid input");

            var jsonBody = JsonSerializer.Serialize(new
            {
                model = typed.Model,
                prompt = typed.Prompt,
                height = typed.Height,
                width = typed.Width,
                seconds = typed.Seconds,
                fps = typed.Fps,
                seed = typed.Seed,
                output_format = "MP4",
                negative_prompt = typed.NegativePrompt
            });

            using var client = clientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);
            using var req = new HttpRequestMessage(HttpMethod.Post, "https://api.together.xyz/v2/videos")
            {
                Content = new StringContent(jsonBody, Encoding.UTF8, "application/json")
            };

            using var resp = await client.SendAsync(req, cancellationToken);
            var json = await resp.Content.ReadAsStringAsync(cancellationToken);
            if (!resp.IsSuccessStatusCode)
                throw new Exception($"{resp.StatusCode}: {json}");

            return await JsonNode.ParseAsync(BinaryData.FromString(json).ToStream());
        }));

    [Description("Fetch metadata and output URL for a generated Together AI video.")]
    [McpServerTool(Title = "Get Together AI Video", Name = "together_video_get", ReadOnly = true)]
    public static async Task<CallToolResult?> TogetherVideo_Get(
        [Description("The ID of the video job to fetch.")] string id,
        IServiceProvider serviceProvider,
        RequestContext<CallToolRequestParams> requestContext,
        CancellationToken cancellationToken = default)
        => await requestContext.WithExceptionCheck(async () =>
        await requestContext.WithStructuredContent(async () =>
        {
            ArgumentNullException.ThrowIfNullOrWhiteSpace(id);

            var settings = serviceProvider.GetRequiredService<TogetherSettings>();
            var clientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();

            using var client = clientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);

            using var resp = await client.GetAsync($"https://api.together.xyz/v2/videos/{id}", cancellationToken);
            var json = await resp.Content.ReadAsStringAsync(cancellationToken);
            if (!resp.IsSuccessStatusCode)
                throw new Exception($"{resp.StatusCode}: {json}");

            return await JsonNode.ParseAsync(BinaryData.FromString(json).ToStream());
        }));

    [Description("Please fill in the Together AI video generation request details.")]
    public class TogetherNewVideo
    {
        [JsonPropertyName("prompt")]
        [Required]
        [Description("Prompt describing the video to generate.")]
        public string Prompt { get; set; } = default!;

        [JsonPropertyName("model")]
        [Required]
        [Description("Video model to use, e.g. black-forest-labs/FLUX.1-schnell-video.")]
        public string Model { get; set; } = default!;

        [JsonPropertyName("seconds")]
        [Description("Video duration in seconds (default 6).")]
        public int Seconds { get; set; } = 6;

        [JsonPropertyName("fps")]
        [Description("Frames per second (default 24).")]
        public int Fps { get; set; } = 24;

        [JsonPropertyName("width")]
        [Description("Width of the video in pixels (default 1024).")]
        public int Width { get; set; } = 1024;

        [JsonPropertyName("height")]
        [Description("Height of the video in pixels (default 1024).")]
        public int Height { get; set; } = 1024;

        [JsonPropertyName("negative_prompt")]
        [Description("Optional negative prompt to exclude elements from generation.")]
        public string? NegativePrompt { get; set; }

        [JsonPropertyName("seed")]
        [Description("Optional random seed for deterministic generation.")]
        public int? Seed { get; set; }

        [JsonPropertyName("filename")]
        [Description("Filename (without extension). Defaults to autogenerated name.")]
        public string? Filename { get; set; }
    }
}
