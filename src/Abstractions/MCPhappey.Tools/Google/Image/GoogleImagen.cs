using System.ComponentModel;
using System.Net.Mime;
using MCPhappey.Common.Extensions;
using MCPhappey.Core.Extensions;
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
    public static async Task<CallToolResult?> GoogleImagen_CreateImage(
        [Description("prompt")]
        string prompt,
        IServiceProvider serviceProvider,
        RequestContext<CallToolRequestParams> requestContext,
        [Description("AI image model: imagen-3.0-generate-002, imagen-4.0-ultra-generate-preview-06-06 or imagen-4.0-generate-preview-06-06")]
        string imageModel = "imagen-3.0-generate-002",
        [Description("The aspect ratio of the generated image. 1:1, 9:16, 16:9, 3:4, or 4:3")]
        string aspectRatio = "1:1",
        [Description("The number of images to generate. Max. 4.")]
        int numberOfImages = 1,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(prompt);
        var googleAI = serviceProvider.GetRequiredService<Mscc.GenerativeAI.GoogleAI>();

        var imageClient = googleAI.ImageGenerationModel(imageModel);
        
        try
        {
            var item = await imageClient.GenerateImages(new Mscc.GenerativeAI.ImageGenerationRequest(prompt, 1)
            {
                Parameters = new()
                {
                    SampleCount = numberOfImages,
                    AspectRatio = aspectRatio,
                    OutputOptions = new Mscc.GenerativeAI.OutputOptions()
                    {
                        MimeType = MediaTypeNames.Image.Png
                    }
                }
            }, cancellationToken);

            List<ResourceLinkBlock> resourceLinks = [];

            foreach (var imageItem in item.Predictions)
            {
                var outputName = $"GoogleImagen_CreateImage_{DateTime.Now.Ticks}.png";
                var result = await requestContext.Server.Upload(serviceProvider, outputName,
                    BinaryData.FromBytes(Convert.FromBase64String(imageItem.BytesBase64Encoded!)), cancellationToken);

                if (result != null) resourceLinks.Add(result);
            }

            return resourceLinks?.ToResourceLinkCallToolResponse();
        }
        catch (Exception e)
        {
            return e.Message.ToErrorCallToolResponse();
        }
    }
}

