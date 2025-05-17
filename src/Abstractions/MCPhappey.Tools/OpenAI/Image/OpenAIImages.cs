using System.ComponentModel;
using MCPhappey.Core.Extensions;
using MCPhappey.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using OAI = OpenAI;

namespace MCPhappey.Tools.OpenAI.Image;

public static class OpenAIImages
{
    [Description("Create a image with OpenAI image generator")]
    public static async Task<CallToolResponse> OpenAIImages_CreateImage(
        [Description("prompt")]
        string prompt,
        IServiceProvider serviceProvider,
        IMcpServer mcpServer,
        [Description("Size of the image")]
        string? size = "1024x1024",
        [Description("Style of the image")]
        string? style = "vivid",
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(prompt);
        var uploadService = serviceProvider.GetRequiredService<UploadService>();
        var openAiClient = serviceProvider.GetOpenAiClient();

        var sizeValue = size switch
        {
            "1024x1024" => OAI.Images.GeneratedImageSize.W1024xH1024,
            "1792x1024" => OAI.Images.GeneratedImageSize.W1792xH1024,
            "1024x1792" => OAI.Images.GeneratedImageSize.W1024xH1792,
            _ => OAI.Images.GeneratedImageSize.W1024xH1024
        };

        var styleValue = style switch
        {
            "vivid" => OAI.Images.GeneratedImageStyle.Vivid,
            "natural" => OAI.Images.GeneratedImageStyle.Natural,
            _ => OAI.Images.GeneratedImageStyle.Vivid
        };

        var imageClient = openAiClient.GetImageClient("dall-e-3");

        var item = await imageClient.GenerateImageAsync(prompt, new()
        {
            Quality = new OAI.Images.GeneratedImageQuality("hd"),
            Size = sizeValue,
            Style = styleValue,
            ResponseFormat = OAI.Images.GeneratedImageFormat.Bytes
        }, cancellationToken);

        var uploaded = await uploadService.UploadToRoot(mcpServer,serviceProvider, $"OpenAI-Image-{DateTime.Now.Ticks}.png",
            item.Value.ImageBytes, cancellationToken);

        return new CallToolResponse()
        {
            Content = [new Content(){
                Type = "image",
                MimeType = "image/png",
                Data = Convert.ToBase64String(item.Value.ImageBytes)
            }]
        };
    }
}

