using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Net.Mime;
using System.Text.Json.Serialization;
using MCPhappey.Common.Extensions;
using MCPhappey.Core.Extensions;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace MCPhappey.Tools.Google.Image;

public static class GoogleImagen
{
    [Description("Create a image with Google Imagen image generator")]
    [McpServerTool(Title = "Generate image with Google Imagen", Destructive = false)]
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
        [Description("New image file name, without extension")]
        string? filename = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(prompt);
        var googleAI = serviceProvider.GetRequiredService<Mscc.GenerativeAI.GoogleAI>();

        var imageClient = googleAI.ImageGenerationModel(imageModel);

        try
        {
            var (typed, notAccepted) = await requestContext.Server.TryElicit(
                           new GoogleImagenNewImage
                           {
                               Prompt = prompt,
                               NumberOfImages = numberOfImages,
                               Filename = filename ?? requestContext.ToOutputFileName()
                           },

                           cancellationToken);

            if (notAccepted != null) return notAccepted;
            if (typed == null) return "Something went wrong".ToErrorCallToolResponse();

            var item = await imageClient.GenerateImages(new Mscc.GenerativeAI.ImageGenerationRequest(typed.Prompt, typed.NumberOfImages)
            {
                Parameters = new()
                {
                    SampleCount = typed.NumberOfImages,
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
                var result = await requestContext.Server.Upload(serviceProvider,
                     $"{typed?.Filename}.png",
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


    [Description("Please fill in the AI image request details.")]
    public class GoogleImagenNewImage
    {
        [JsonPropertyName("prompt")]
        [Required]
        [Description("The image prompt.")]
        public string Prompt { get; set; } = default!;

        [JsonPropertyName("filename")]
        [Required]
        [Description("The new image file name.")]
        public string Filename { get; set; } = default!;

        [JsonPropertyName("numberOfImages")]
        [Required]
        [Range(1, 4)]
        [Description("The number of images to create.")]
        public int NumberOfImages { get; set; } = 1;
    }
}

