using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using MCPhappey.Common.Extensions;
using MCPhappey.Core.Extensions;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace MCPhappey.Tools.Google.Video;

public static class GoogleVideo
{
    [Description("Prompt a YouTube video using Google Gemini AI.")]
    [McpServerTool(
        Destructive = false,
        Title = "Prompt YouTube video with Gemini",
        ReadOnly = true)]
    public static async Task<CallToolResult?> GoogleVideo_PromptYouTube(
        [Description("Prompt or instruction for the Gemini model (e.g. 'Summarize the video', 'Extract action points', etc.)")]
        string prompt,
        [Description("YouTube video URL")]
        string url,
        IServiceProvider serviceProvider,
        RequestContext<CallToolRequestParams> requestContext,
        CancellationToken cancellationToken = default) =>
        await requestContext.WithExceptionCheck(async () =>
        {
            ArgumentNullException.ThrowIfNullOrWhiteSpace(prompt);
            ArgumentNullException.ThrowIfNullOrWhiteSpace(url);

            var googleAI = serviceProvider.GetRequiredService<Mscc.GenerativeAI.GoogleAI>();
            var googleClient = googleAI.GenerativeModel("gemini-2.5-flash");

            var (typed, notAccepted, result) = await requestContext.Server.TryElicit(
                        new GoogleVideoPromptYoutTube
                        {
                            Prompt = prompt,
                            YouTubeUrl = url
                        },

                        cancellationToken);

            if (notAccepted != null) return notAccepted;
            if (typed == null) return "Something went wrong".ToErrorCallToolResponse();

            var graphItem = await googleClient.GenerateContent(new Mscc.GenerativeAI.GenerateContentRequest()
            {
                Contents =
                [
                    new Mscc.GenerativeAI.Content(typed.Prompt)
                        {
                            Parts = [
                                new Mscc.GenerativeAI.FileData() {
                                    FileUri = typed.YouTubeUrl
                                }
                            ]
                        }
                ]
            }, cancellationToken: cancellationToken);

            return graphItem?.Text?.ToTextCallToolResponse();

        });

    /*[Description("Create a video with Google Veo video generator")]
    [McpServerTool(Title = "Generate video with Google Veo", Destructive = false)]
    public static async Task<CallToolResult?> GoogleVeo_CreateVideo(
          [Description("prompt")]
        string prompt,
          IServiceProvider serviceProvider,
          RequestContext<CallToolRequestParams> requestContext,
          [Description("AI video model: veo-3.0-generate-preview")]
        string videoModel = "veo-3.0-generate-preview",
          //    [Description("The number of videos to generate. Max. 4.")]
          //  int numberOfImages = 1,
          [Description("New video file name, without extension")]
        string? filename = null,
          CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(prompt);
        var googleAI = serviceProvider.GetRequiredService<Mscc.GenerativeAI.GoogleAI>();

        var imageClient = googleAI.GenerativeModel(videoModel, new Mscc.GenerativeAI.GenerationConfig()
        {
                
         });

        try
        {
            var (typed, notAccepted, result) = await requestContext.Server.TryElicit(
                           new GoogleVeoNewVideo
                           {
                               Prompt = prompt,
                               Filename = filename ?? requestContext.ToOutputFileName()
                           }, cancellationToken);

            if (notAccepted != null) return notAccepted;
            if (typed == null) return "Something went wrong".ToErrorCallToolResponse();
            var item = await imageClient.GenerateVideos(new Mscc.GenerativeAI.GenerateVideosRequest(typed.Prompt)
            {
            //  Instances = [new Mscc.GenerativeAI.Instance() { Prompt = typed.Prompt} ]
            });

            List<ResourceLinkBlock> resourceLinks = [];

            foreach (var imageItem in item.GeneratedVideos ?? [])
            {
                var graphItem = await requestContext.Server.Upload(serviceProvider,
                     $"{typed?.Filename}.mp4",
                    BinaryData.FromBytes(imageItem.Video.VideoBytes), cancellationToken);

                if (graphItem != null) resourceLinks.Add(graphItem);
            }

            return resourceLinks?.ToResourceLinkCallToolResponse();
        }
        catch (Exception e)
        {
            return e.Message.ToErrorCallToolResponse();
        }
    }


    [Description("Please fill in the AI video request details.")]
    public class GoogleVeoNewVideo
    {
        [JsonPropertyName("prompt")]
        [Required]
        [Description("The video prompt.")]
        public string Prompt { get; set; } = default!;

        [JsonPropertyName("filename")]
        [Required]
        [Description("The new image file name.")]
        public string Filename { get; set; } = default!;
    }
*/
    [Description("Please fill in the YouTube prompt details.")]
    public class GoogleVideoPromptYoutTube
    {
        [JsonPropertyName("prompt")]
        [Required]
        [Description("The YouTube video question prompt.")]
        public string Prompt { get; set; } = default!;

        [JsonPropertyName("youTubeUrl")]
        [Required]
        [Description("YouTube url.")]
        public string YouTubeUrl { get; set; } = default!;

    }
}