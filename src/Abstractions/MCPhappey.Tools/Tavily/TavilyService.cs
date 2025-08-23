using System.ComponentModel;
using MCPhappey.Common.Extensions;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace MCPhappey.Tools.Tavily;

public static class TavilyService
{
    [Description("Execute a search query using Tavily Search.")]
    [McpServerTool(Title = "Tavily search",
        ReadOnly = true, OpenWorld = true)]
    public static async Task<CallToolResult?> Tavily_Search(
        string query,
        IServiceProvider serviceProvider,
        [Description("The maximum number of search results to return.")] int? maxResults = 5,
        [Description("Will return all results after the specified start date ( publish date ). Required to be written in the format YYYY-MM-DD")] string? startDate = null,
        [Description("Will return all results before the specified end date ( publish date ). Required to be written in the format YYYY-MM-DD")] string? endDate = null,
        [Description("Also perform an image search and include the results in the response.")] bool? includeImages = false,
        [Description("When include_images is true, also add a descriptive text for each image.")] bool? includeImageDescriptions = false,
        CancellationToken cancellationToken = default)
    {
        var tavily = serviceProvider.GetRequiredService<ITavilyClient>();
        var json = await tavily.SearchAsync(query, maxResults, startDate, endDate,
            includeImages, includeImageDescriptions, ct: cancellationToken);

        return json.ToJsonCallToolResponse("https://api.tavily.com/search");
    }

    [Description("Extract web page content from one or more specified URLs using Tavily Extract.")]
    [McpServerTool(
      Title = "Tavily extract",
      ReadOnly = true,
      OpenWorld = true)]
    public static async Task<CallToolResult?> Tavily_Extract(
      IServiceProvider serviceProvider,
      [Description("List of absolute URLs (http/https) to extract content from.")] IEnumerable<string> urls,
      [Description("Include image URLs discovered on the pages.")] bool? includeImages = false,
      CancellationToken cancellationToken = default)
    {
        var urlList = (urls ?? Array.Empty<string>())
            .Select(u => u?.Trim())
            .Where(u => !string.IsNullOrWhiteSpace(u))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OfType<string>()
            .ToList();

        if (urlList.Count == 0)
            return "At least one URL is required.".ToErrorCallToolResponse();

        var tavily = serviceProvider.GetRequiredService<ITavilyClient>();
        var json = await tavily.ExtractAsync(
            urls: urlList,
            includeImages: includeImages,
            ct: cancellationToken);

        return json.ToJsonCallToolResponse("https://api.tavily.com/extract");
    }

    [Description("Tavily Crawl is a graph-based website traversal tool that can explore hundreds of paths in parallel with built-in extraction and intelligent discovery.")]
    [McpServerTool(
        Title = "Tavily crawl",
        ReadOnly = true,
        OpenWorld = true)]
    public static async Task<CallToolResult?> Tavily_Crawl(
        IServiceProvider serviceProvider,
        [Description("Absolute URL (http/https) to crawl.")] string url,
        CancellationToken cancellationToken = default)
    {
        url = url?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(url))
            "url is required".ToErrorCallToolResponse();

        var tavily = serviceProvider.GetRequiredService<ITavilyClient>();
        var json = await tavily.CrawlAsync(url, cancellationToken);

        return json.ToJsonCallToolResponse("https://api.tavily.com/crawl");
    }

    [Description("Tavily Map traverses websites like a graph and can explore hundreds of paths in parallel with intelligent discovery to generate comprehensive site maps.")]
    [McpServerTool(
        Title = "Tavily map",
        ReadOnly = true,
        OpenWorld = true)]
    public static async Task<CallToolResult?> Tavily_Map(
        IServiceProvider serviceProvider,
        RequestContext<CallToolRequestParams> requestContext,
        [Description("Absolute URL (http/https) to map (typically a site root or section).")] string url,
        CancellationToken cancellationToken = default)
    {
        url = url?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(url))
            "url is required.".ToErrorCallToolResponse();

        var tavily = serviceProvider.GetRequiredService<ITavilyClient>();
        var json = await tavily.MapAsync(url, cancellationToken);

        return json.ToJsonCallToolResponse("https://api.tavily.com/map");
    }
}

