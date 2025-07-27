using System.ComponentModel;
using MCPhappey.Common.Extensions;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace MCPhappey.Tools.Google.Video;

public static class GoogleVideo
{
    [Description("Prompt a YouTube video using Google Gemini AI.")]
    [McpServerTool(
        Name = "GoogleVideo_PromptYouTube",
        Title = "Prompt YouTube video with Gemini",
        ReadOnly = true)]
    public static async Task<CallToolResult?> GoogleVideo_PromptYouTube(
        [Description("Prompt or instruction for the Gemini model (e.g. 'Summarize the video', 'Extract action points', etc.)")]
        string prompt,
        [Description("YouTube video URL")]
        string url,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(prompt);
        ArgumentNullException.ThrowIfNullOrWhiteSpace(url);

        var googleAI = serviceProvider.GetRequiredService<Mscc.GenerativeAI.GoogleAI>();
        var googleClient = googleAI.GenerativeModel("gemini-2.5-flash");

        try
        {
            var result = await googleClient.GenerateContent(new Mscc.GenerativeAI.GenerateContentRequest()
            {
                Contents =
                [
                    new Mscc.GenerativeAI.Content(prompt)
                    {
                        Parts = [
                            new Mscc.GenerativeAI.FileData() {
                                FileUri = url
                            }
                        ]
                    }
                ]
            }, cancellationToken: cancellationToken);

            return result?.Text?.ToTextCallToolResponse();
        }
        catch (Exception ex)
        {
            return ex.Message.ToErrorCallToolResponse();
        }
    }
}