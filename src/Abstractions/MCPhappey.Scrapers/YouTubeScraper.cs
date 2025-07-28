using MCPhappey.Common;
using MCPhappey.Common.Models;
using ModelContextProtocol.Server;

namespace MCPhappey.Scrapers.Generic;

public class YouTubeScraper(Mscc.GenerativeAI.GoogleAI googleAI) : IContentScraper
{
    public bool SupportsHost(ServerConfig currentConfig, string url)
    {
        // Acceptable YouTube hosts
        var validHosts = new[]
        {
        "youtube.com",
        "www.youtube.com",
        "youtu.be"
        };

        var uri = new Uri(url);
        var host = uri.Host.ToLowerInvariant();

        // Match on canonical host or subdomain (e.g., m.youtube.com)
        bool isYoutube = validHosts.Any(h => host == h || host.EndsWith("." + h));

        return isYoutube;
    }


    public async Task<IEnumerable<FileItem>?> GetContentAsync(IMcpServer mcpServer, IServiceProvider serviceProvider,
         string url, CancellationToken cancellationToken = default)
    {
        var googleClient = googleAI.GenerativeModel("gemini-2.5-flash");
        var result = await googleClient.GenerateContent(new Mscc.GenerativeAI.GenerateContentRequest()
        {
            Contents =
            [
                new Mscc.GenerativeAI.Content(
                    "Analyze the following YouTube video and generate a very comprehensive, detailed overview of its spoken content. Your output should be as close to a full transcript as possible, summarizing all key points, arguments, and examples, with minimal omissions. If relevant, include timestamps or section markers. Do not invent content."
                ) {
                    Parts = [
                        new Mscc.GenerativeAI.FileData() {
                            FileUri = url
                        }

                    ]
                },
            ]
        }, cancellationToken: cancellationToken);

        return [new FileItem() {
            Uri = url,
            MimeType = "text/plain",
            Contents = BinaryData.FromString(result?.Text ?? string.Empty)
         }];
    }
}
