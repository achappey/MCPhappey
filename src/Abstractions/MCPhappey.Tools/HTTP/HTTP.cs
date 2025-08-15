using System.ComponentModel;
using HtmlAgilityPack;
using MCPhappey.Common.Extensions;
using MCPhappey.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using HtmlAgilityPack.CssSelectors.NetCore;

namespace MCPhappey.Tools.HTTP;

public static class HTTPService
{
    [Description("Fetches a public accessible url.")]
    [McpServerTool(Title = "Fetch public URL",
        Destructive = false,
        Idempotent = true,
        ReadOnly = true)]
    public static async Task<IEnumerable<ContentBlock>> Http_FetchUrl(
        [Description("The url to fetch.")]
        string url,
        IServiceProvider serviceProvider,
        IMcpServer mcpServer,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(url);

        var downloadService = serviceProvider.GetRequiredService<DownloadService>();

        var content = await downloadService.ScrapeContentAsync(serviceProvider, mcpServer, url,
            cancellationToken) ?? throw new Exception();

        return content.ToContentBlocks();
    }

    [Description("Fetches raw HTML from a public accessible url.")]
    [McpServerTool(Title = "Fetch raw HTML from public URL",
        Destructive = false,
        Idempotent = true,
        ReadOnly = true)]
    public static async Task<IEnumerable<ContentBlock>> Http_FetchHtml(
        [Description("The url to fetch.")]
        string url,
        IServiceProvider serviceProvider,
        IMcpServer mcpServer,
        [Description("Css selector. Just raw, dont include select.")]
        string? cssSelector = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(url);

        var downloadService = serviceProvider.GetRequiredService<DownloadService>();

        var content = await downloadService.DownloadContentAsync(serviceProvider, mcpServer, url,
            cancellationToken) ?? throw new Exception();

        // Geen selector: hele pagina teruggeven als 1 HTML-resource
        if (string.IsNullOrWhiteSpace(cssSelector))
        {
            return content.ToContentBlocks();
        }

        List<string> results = [];

        foreach (var item in content.Where(a => a.MimeType == "text/html"))
        {
            // Met CSS-selector: matches parsen met HtmlAgilityPack + Fizzler
            var doc = new HtmlDocument
            {
                OptionFixNestedTags = true,
                OptionAutoCloseOnEnd = true
            };

            doc.LoadHtml(item.Contents.ToString());

            var matches = doc.DocumentNode.QuerySelectorAll(cssSelector?.Trim());
            if (matches == null || !matches.Any())
                return Array.Empty<ContentBlock>();

            // Eén block per match (raw outerHTML)
            var blocks = matches
                .Select((n, i) => n.OuterHtml)
                .ToArray();

            results.AddRange(blocks);
        }

        return [string.Join("\n", results).ToTextContentBlock()];
    }
}

