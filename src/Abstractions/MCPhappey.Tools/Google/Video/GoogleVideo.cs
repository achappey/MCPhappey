using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using MCPhappey.Common.Extensions;
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
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(prompt);
        ArgumentNullException.ThrowIfNullOrWhiteSpace(url);

        var googleAI = serviceProvider.GetRequiredService<Mscc.GenerativeAI.GoogleAI>();
        var googleClient = googleAI.GenerativeModel("gemini-2.5-flash");

        try
        {
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
        }
        catch (Exception ex)
        {
            return ex.Message.ToErrorCallToolResponse();
        }
    }



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