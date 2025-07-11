
using System.Text.RegularExpressions;
using MCPhappey.Core.Extensions;
using MCPhappey.Common.Models;
using Microsoft.KernelMemory.DataFormats.WebPages;
using MCPhappey.Common;
using ModelContextProtocol.Protocol;
using MCPhappey.Common.Extensions;

namespace MCPhappey.Core.Services;

public partial class DownloadService(WebScraper webScraper,
    TransformService transformService,
    IEnumerable<IContentScraper> scrapers) : IWebScraper
{
    public async Task<WebScraperResult> GetContentAsync(string url, CancellationToken cancellationToken = default)
        => await webScraper.GetContentAsync(url, cancellationToken);

    public async Task<IEnumerable<FileItem>> ScrapeContentAsync(IServiceProvider serviceProvider,
        ModelContextProtocol.Server.IMcpServer mcpServer,
        string url,
        CancellationToken cancellationToken = default)
    {
        Uri uri = new(url);
        var serverConfig = serviceProvider.GetServerConfig(mcpServer)
            ?? throw new Exception();

        var supportedScrapers = scrapers
            .Where(a => a.SupportsHost(serverConfig, url));

        IEnumerable<FileItem>? fileContent = null;

        var domain = new Uri(url).Host; // e.g., "example.com"
        var markdown = $"GET [{domain}]({url})";

        await mcpServer.SendMessageNotificationAsync(markdown, LoggingLevel.Info);

        foreach (var decoder in supportedScrapers)
        {
            fileContent = await decoder.GetContentAsync(mcpServer, serviceProvider, url, cancellationToken);

            if (fileContent != null)
            {
                var decodeTasks = fileContent.Select(a => transformService.DecodeAsync(url,
                    a.Contents,
                    a.MimeType, cancellationToken));

                return await Task.WhenAll(decodeTasks);
            }
        }

        var defaultScraper = await webScraper.GetContentAsync(url, cancellationToken);

        if (!defaultScraper.Success)
        {
            throw new Exception(defaultScraper.Error);
        }

      //  var fileItem = defaultScraper.ToFileItem(url);

        return [await transformService.DecodeAsync(url,
                          defaultScraper.Content,
                          defaultScraper.ContentType, cancellationToken)];
    }

    [GeneratedRegex(@"^[^.]+\.crm\d+\.dynamics\.com$", RegexOptions.IgnoreCase | RegexOptions.Compiled, "nl-NL")]
    private static partial Regex DynamicsHostPattern();

}
