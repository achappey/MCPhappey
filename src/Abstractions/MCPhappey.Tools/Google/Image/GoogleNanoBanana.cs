using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;
using MCPhappey.Common.Extensions;
using MCPhappey.Common.Models;
using MCPhappey.Core.Extensions;
using MCPhappey.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace MCPhappey.Tools.Google.Image;

public static class GoogleNanoBanana
{
    [Description("Create a image with Google Nano Banana AI native image generator")]
    [McpServerTool(Title = "Generate image with Nano Banana", Destructive = false, ReadOnly = true)]
    public static async Task<CallToolResult?> GoogleNanoBanana_CreateImage(
        [Description("Image prompt (only English)")]
        string prompt,
        IServiceProvider serviceProvider,
        RequestContext<CallToolRequestParams> requestContext,
        [Description("Optional image url for image edits. Supports protected links like SharePoint and OneDrive links")]
        string? fileUrl = null,
        CancellationToken cancellationToken = default) =>
        await requestContext.WithExceptionCheck(async () =>
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(prompt);
        var googleAI = serviceProvider.GetRequiredService<Mscc.GenerativeAI.GoogleAI>();

        var downloader = serviceProvider.GetRequiredService<DownloadService>();
        var items = !string.IsNullOrEmpty(fileUrl) ? await downloader.DownloadContentAsync(serviceProvider,
            requestContext.Server, fileUrl, cancellationToken) : null;

        var (typed, notAccepted, result) = await requestContext.Server.TryElicit(
               new GoogleNanoBananaNewImage
               {
                   Prompt = prompt,
               },
               cancellationToken);

        if (notAccepted != null) return notAccepted;
        if (typed == null) return "Something went wrong".ToErrorCallToolResponse();

        var resultContent = await requestContext.Server.SampleAsync(new CreateMessageRequestParams()
        {
            Messages = [..items?.Select(a => new SamplingMessage() {
                Role = Role.User,
                Content = new ImageContentBlock() {
                    MimeType = a.MimeType,
                    Data = Convert.ToBase64String(a.Contents.ToArray())
                }
            }) ?? [], new SamplingMessage()
            {
                Role = Role.User,
                Content = new TextContentBlock() {
                    Text = prompt
                }
            }],
            IncludeContext = ContextInclusion.ThisServer,
            MaxTokens = 4096,
            SystemPrompt = "Create a single image according to the prompt",
            ModelPreferences = "gemini-2.5-flash-image"?.ToModelPreferences(),
            Metadata = JsonSerializer.SerializeToElement(new Dictionary<string, object>
                        {
                            { "google", new {

                            } },
                        })
        }, cancellationToken);


        /*   List<ResourceLinkBlock> resourceLinks = [];

           foreach (var imageItem in item.Predictions)
           {
               var graphItem = await requestContext.Server.Upload(serviceProvider,
                    $"{typed?.Filename}.png",
                   BinaryData.FromBytes(Convert.FromBase64String(imageItem.BytesBase64Encoded!)), cancellationToken);

               if (graphItem != null) resourceLinks.Add(graphItem);
           }
   */

        return new CallToolResult()
        {
            Content = [resultContent.Content]
        };

        //      return resourceLinks?.ToResourceLinkCallToolResponse();

    });


    [Description("Please fill in the AI image request details.")]
    public class GoogleNanoBananaNewImage
    {
        [JsonPropertyName("prompt")]
        [Required]
        [Description("The image prompt. English prompts only")]
        public string Prompt { get; set; } = default!;
    }

}

