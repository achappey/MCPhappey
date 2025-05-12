using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using MCPhappey.Auth.Models;
using MCPhappey.Common.Models;
using MCPhappey.Core.Extensions;
using MCPhappey.Core.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Protocol.Types;
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
        ServerConfig serverConfig,
        IServiceProvider serviceProvider,
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
            .GetContentAsync(serverConfig, searchUrl,
            null,
            cancellationToken);

        return (searchUrl, result.Contents.ToString());
    }

    [Description("Search the web with Bing")]
    public static async Task<CallToolResponse> Bing_SearchBing(
        [Description("Search query")]
        string query,
        IServiceProvider serviceProvider,
        IMcpServer mcpServer,
        [Description("Specify the content's freshnes. Return items that Bing discovered within the last x period.")]
        Freshness? freshness = null,
        [Description("The market where the results come from. Typically, market is the country where the user is making the request from. The market must be in the form <language>-<country/region>. For example, en-US.")]
        string? market = null,
        [Description("The language to use for user interface strings. You may specify the language using either a 2-letter or 4-letter code. Using 4-letter codes is preferred.")]
        string? language = null,
        CancellationToken cancellationToken = default)
    {
        var tokenProvider = serviceProvider.GetService<TokenProvider>();
        var samplingService = serviceProvider.GetRequiredService<SamplingService>();
        var config = serviceProvider.GetServerConfig(mcpServer);

        if (mcpServer.ClientCapabilities?.Sampling == null)
        {
            var (url, contents) = await Search(config!, serviceProvider, query, freshness, market, language, cancellationToken);

            return contents.ToJsonCallToolResponse(url);
        }

        var queryArgs = new Dictionary<string, JsonElement>()
        {
            ["topic"] = JsonSerializer.SerializeToElement(query),
            ["numberOfQueries"] = JsonSerializer.SerializeToElement("3"),
        };

        Dictionary<string, string> results = [];

        var querySampling = await samplingService.GetPromptSample<QueryList>(mcpServer, config!, "get-bing-serp-queries", queryArgs,
                    null,
                    "o4-mini", cancellationToken: cancellationToken);

        List<string> queries = [.. querySampling?.Queries ?? [], query];

        var semaphore = new SemaphoreSlim(5);
        var tasks = queries
            .Distinct()
            .Select(async item =>
        {
            await semaphore.WaitAsync();
            try
            {
                return await Search(config!, serviceProvider, item, freshness, market, language, cancellationToken);
            }
            finally
            {
                semaphore.Release();
            }
        }).ToList();

        var queryTaskItem = await Task.WhenAll(tasks);
        var concatenatedResults = string.Join("\n\n", queryTaskItem);

        return await serviceProvider.ExtractWithFacts(mcpServer, concatenatedResults,
            $"https://api.bing.microsoft.com/v7.0/search?q={query}&responseFilter=webpages,news",
             query, 5, cancellationToken);
    }
}

public class QueryList
{
    [JsonPropertyName("queries")]
    public IEnumerable<string> Queries { get; set; } = null!;
}