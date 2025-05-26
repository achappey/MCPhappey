using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using MCPhappey.Core.Extensions;
using MCPhappey.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace MCPhappey.Tools.Bing;

public static class BingWebSearch
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum Freshness
    {
        week,
        month
    }

    private static async Task<(string url, string contents)> Search(
        IServiceProvider serviceProvider,
        IMcpServer mcpServer,
        string query, Freshness? freshness, string? market, string? language, CancellationToken cancellationToken = default)
    {
        var downloadService = serviceProvider.GetRequiredService<DownloadService>();
        string freshnessParam = string.Empty;
        string marketParam = string.Empty;
        string langParam = string.Empty;

        if (freshness != null)
        {
            freshnessParam = $"&freshness={Enum.GetName(freshness.Value)?.ToLowerInvariant()}";
        }

        if (!string.IsNullOrEmpty(market))
        {
            marketParam = $"&mkt={market?.ToString()}";
        }

        if (!string.IsNullOrEmpty(language))
        {
            langParam = $"&setLang={language?.ToString()}";
        }

        var searchUrl = $"https://api.bing.microsoft.com/v7.0/search?q={query}&responseFilter=webpages,news{freshnessParam}{marketParam}{langParam}";

        var result = await downloadService
            .ScrapeContentAsync(serviceProvider, mcpServer, searchUrl,
            cancellationToken);

        return (searchUrl, result.FirstOrDefault()?.Contents.ToString() ?? string.Empty);
    }

    [Description("Search the web with Bing")]
    [McpServerTool(ReadOnly = true)]
    public static async Task<CallToolResponse> Bing_SearchBing(
        [Description("Search query")]
        string query,
        IServiceProvider serviceProvider,
        RequestContext<CallToolRequestParams> requestContext,
        [Description("Specify the content's freshnes. Return items that Bing discovered within the last x period.")]
        Freshness? freshness = null,
        [Description("The market where the results come from. Typically, market is the country where the user is making the request from. The market must be in the form <language>-<country/region>. For example, en-US.")]
        string? market = null,
        [Description("The language to use for user interface strings. You may specify the language using either a 2-letter or 4-letter code. Using 4-letter codes is preferred.")]
        string? language = null,
        CancellationToken cancellationToken = default)
    {
        var samplingService = serviceProvider.GetRequiredService<SamplingService>();
        var mcpServer = requestContext.Server;
        var config = serviceProvider.GetServerConfig(mcpServer);
        //ProgressToken? progressToken = requestContext.Params?.Meta?.ProgressToken.HasValue == true ?
        //    new ProgressToken(requestContext.Params?.Meta?.ProgressToken.Value.Token?.ToString()!) : null;
        int? progressCounter = requestContext.Params?.Meta?.ProgressToken is not null ? 1 : null;

        if (mcpServer.ClientCapabilities?.Sampling == null)
        {
            var (url, contents) = await Search(serviceProvider, mcpServer,
                query, freshness, market, language, cancellationToken);

            return contents.ToJsonCallToolResponse(url);
        }

        var queryArgs = new Dictionary<string, JsonElement>()
        {
            ["topic"] = JsonSerializer.SerializeToElement(query),
            ["numberOfQueries"] = JsonSerializer.SerializeToElement("3"),
        };

        Dictionary<string, string> results = [];

        var querySampling = await samplingService.GetPromptSample<QueryList>(serviceProvider,
            mcpServer, "get-bing-serp-queries", queryArgs,
                "gpt-4.1-mini", cancellationToken: cancellationToken);

        if (requestContext.Params?.Meta?.ProgressToken is not null)
        {
            await mcpServer.SendNotificationAsync("notifications/progress", new ProgressNotification()
            {
                ProgressToken = requestContext.Params.Meta.ProgressToken.Value,
                Progress = new ProgressNotificationValue()
                {
                    Progress = progressCounter!.Value!,
                    Message = $"Expanded to {querySampling?.Queries.Count()} queries:\n{string.Join("\n", querySampling?.Queries ?? [])}"
                },
            }, cancellationToken: cancellationToken);
        }

        List<string> queries = [.. querySampling?.Queries ?? [], query];

        var semaphore = new SemaphoreSlim(5);
        var tasks = queries
            .Distinct()
            .Select(async item =>
        {
            await semaphore.WaitAsync();
            try
            {
                return await Search(serviceProvider, mcpServer, item, freshness, market, language, cancellationToken);
            }
            finally
            {
                semaphore.Release();
            }
        }).ToList();

        var queryTaskItem = await Task.WhenAll(tasks);
        var concatenatedResults = string.Join("\n\n", queryTaskItem);

        return await serviceProvider.ExtractWithFacts(mcpServer, concatenatedResults,
            $"https://www.bing.com/search?q={query}&responseFilter=webpages,news",
             query, requestContext.Params?.Meta?.ProgressToken, progressCounter, 5, cancellationToken);
    }
}

public class QueryList
{
    [JsonPropertyName("queries")]
    public IEnumerable<string> Queries { get; set; } = null!;
}