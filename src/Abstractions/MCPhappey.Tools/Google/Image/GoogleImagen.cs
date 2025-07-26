using System.ComponentModel;
using System.Net.Mime;
using MCPhappey.Common.Extensions;
using MCPhappey.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace MCPhappey.Tools.Google.Image;

public static class GoogleImagen
{
    [Description("Create a image with Google Imagen image generator")]
    [McpServerTool(Name = "GoogleImagen_CreateImage",
        Title = "Generate image with Google Imagen",
        ReadOnly = true)]
    public static async Task<CallToolResult> GoogleImagen_CreateImage(
        [Description("prompt")]
        string prompt,
        IServiceProvider serviceProvider,
        [Description("AI image model: imagen-3.0-generate-002, imagen-4.0-ultra-generate-preview-06-06 or imagen-4.0-generate-preview-06-06")]
        string imageModel = "imagen-3.0-generate-002",
        [Description("The aspect ratio of the generated image. 1:1, 9:16, 16:9, 3:4, or 4:3")]
        string aspectRatio = "1:1",
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(prompt);
        var uploadService = serviceProvider.GetRequiredService<UploadService>();
        var googleAI = serviceProvider.GetRequiredService<Mscc.GenerativeAI.GoogleAI>();

        var imageClient = googleAI.ImageGenerationModel(imageModel);
        try
        {
            var item = await imageClient.GenerateImages(new Mscc.GenerativeAI.ImageGenerationRequest(prompt, 1)
            {
                Parameters = new()
                {
                    SampleCount = 1,
                    AspectRatio = aspectRatio,
                    OutputOptions = new Mscc.GenerativeAI.OutputOptions()
                    {
                        MimeType = MediaTypeNames.Image.Png
                    }
                }
            }, cancellationToken);

            var content = item.Predictions.Select(a => new ImageContentBlock()
            {
                MimeType = MediaTypeNames.Image.Png,
                Data = a.BytesBase64Encoded!
            }).ToCallToolResult();

            return content;
        }
        catch (Exception e)
        {
            return e.Message.ToErrorCallToolResponse();
        }
    }
}

