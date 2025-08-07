using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using MCPhappey.Common.Extensions;
using MCPhappey.Core.Extensions;
using MCPhappey.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using OpenAI;
using OpenAI.Images;

namespace MCPhappey.Tools.OpenAI.Image;

public static class OpenAIImages
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ImageQuality
    {
        low,
        medium,
        high,
        auto
    }

    [Description("Create a image with OpenAI image generator")]
    [McpServerTool(Name = "OpenAIImages_CreateImage", Title = "Generate image with OpenAI")]
    public static async Task<CallToolResult?> OpenAIImages_CreateImage(
        [Description("prompt")]
        string prompt,
        IServiceProvider serviceProvider,
        RequestContext<CallToolRequestParams> requestContext,
        [Description("Size of the image (1024x1024, 1536x1024 or 1024x1536)")]
        string? size = "1024x1024",
        [Description("New image file name, without extension")]
        string? filename = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(prompt);
        var uploadService = serviceProvider.GetRequiredService<UploadService>();
        var openAiClient = serviceProvider.GetRequiredService<OpenAIClient>();

        var sizeValue = size switch
        {
            "1024x1024" => GeneratedImageSize.W1024xH1024,
            "1536x1024" => new GeneratedImageSize(1536, 1024),
            "1024x1536" => new GeneratedImageSize(1024, 1536),
            _ => GeneratedImageSize.W1024xH1024
        };

        var (typed, notAccepted) = await requestContext.Server.TryElicit(
                    new OpenAINewImage { Prompt = prompt, Filename = filename ?? requestContext.ToOutputFileName() },
                    cancellationToken);

        if (notAccepted != null) return notAccepted;

        var imageClient = openAiClient.GetImageClient("gpt-image-1");
        var selectedQuality = ImageQuality.high;
        var item = await imageClient.GenerateImageAsync(typed?.Prompt, new()
        {
            Quality = new GeneratedImageQuality(Enum.GetName(selectedQuality)!),
            Size = sizeValue,
        }, cancellationToken);

        var result = await requestContext.Server.Upload(serviceProvider,
            $"{typed?.Filename}.png",
            item.Value.ImageBytes, cancellationToken);

        return result?.ToResourceLinkCallToolResponse();
    }


    [Description("Please fill in the AI image request details.")]
    public class OpenAINewImage
    {
        [JsonPropertyName("prompt")]
        [Required]
        [Description("The image prompt.")]
        public string Prompt { get; set; } = default!;

        [JsonPropertyName("filename")]
        [Required]
        [Description("The new image file name.")]
        public string Filename { get; set; } = default!;
    }
}

