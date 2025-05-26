using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using MCPhappey.Core.Extensions;
using MCPhappey.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
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
    public static async Task<CallToolResponse> OpenAIImages_CreateImage(
        [Description("prompt")]
        string prompt,
        IServiceProvider serviceProvider,
        IMcpServer mcpServer,
        [Description("Size of the image (1024x1024, 1536x1024 or 1024x1536)")]
        string? size = "1024x1024",
        [Description("Quality of the image")]
        ImageQuality? quality = ImageQuality.auto,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(prompt);
        var uploadService = serviceProvider.GetRequiredService<UploadService>();
        var openAiClient = serviceProvider.GetOpenAiClient();

        var sizeValue = size switch
        {
            "1024x1024" => GeneratedImageSize.W1024xH1024,
            "1536x1024" => new GeneratedImageSize(1536, 1024),
            "1024x1536" => new GeneratedImageSize(1024, 1536),
            _ => GeneratedImageSize.W1024xH1024
        };

        var imageClient = openAiClient.GetImageClient("gpt-image-1");
        var selectedQuality = quality ?? ImageQuality.auto;
        var item = await imageClient.GenerateImageAsync(prompt, new()
        {
            Quality = new GeneratedImageQuality(Enum.GetName(selectedQuality)!),
            Size = sizeValue,
        }, cancellationToken);

        List<Content> content = [new Content(){
                Type = "image",
                MimeType = "image/png",
                Data = Convert.ToBase64String(item.Value.ImageBytes)
            }];

        var uploaded = await uploadService.UploadToRoot(mcpServer, serviceProvider, $"OpenAI-Image-{DateTime.Now.Ticks}.png",
            item.Value.ImageBytes, cancellationToken);

        if (uploaded != null)
        {
            content.Add(new Content()
            {
                Resource = new TextResourceContents()
                {
                    MimeType = uploaded.MimeType,
                    Uri = uploaded.Uri,
                    Text = JsonSerializer.Serialize(uploaded)
                }
            });
        }

        return new CallToolResponse()
        {
            Content = content
        };
    }
}

