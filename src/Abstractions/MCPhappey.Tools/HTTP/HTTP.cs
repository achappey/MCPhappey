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
        Idempotent = true,
        ReadOnly = true)]
    public static async Task<IEnumerable<ContentBlock>> Http_FetchHtml(
        [Description("The url to fetch.")]
        string url,
        IServiceProvider serviceProvider,
        IMcpServer mcpServer,
        [Description("Css selector.")]
        string? cssSelector = null,
        [Description("Extract only a specific HTML attribute from matched nodes (e.g., href, src, value).")]
        string? attribute = null,
        [Description("Return only the inner text without HTML tags.")]
        bool textOnly = false,
        [Description("Limit the number of matches returned.")]
        int? maxMatches = null,
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
                return [];

            if (maxMatches.HasValue)
                matches = [.. matches.Take(maxMatches.Value)];

            foreach (var match in matches)
            {
                string output;
                if (!string.IsNullOrWhiteSpace(attribute))
                {
                    output = match.GetAttributeValue(attribute, string.Empty);
                }
                else if (textOnly)
                {
                    output = match.InnerText;
                }
                else
                {
                    output = match.OuterHtml;
                }

                if (!string.IsNullOrEmpty(output))
                    results.Add(output);
            }
        }

        return [string.Join("\n", results).ToTextContentBlock()];
    }
}

