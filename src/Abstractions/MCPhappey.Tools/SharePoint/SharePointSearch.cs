using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using MCPhappey.Common.Models;
using MCPhappey.Core.Extensions;
using MCPhappey.Core.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Graph.Beta;
using Microsoft.Graph.Beta.Models;
using Microsoft.Graph.Beta.Search.Query;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace MCPhappey.Tools.SharePoint;

public static class SharePointSearch
{
    /// <summary>
    /// Executes a Microsoft Graph search request for the supplied query and entity types.
    /// </summary>
    private static async Task<string?> SearchContent(
        this GraphServiceClient graphServiceClient,
        string query,
        IReadOnlyList<EntityType?> entityTypes,
        int from = 0,
        int size = 10,
        CancellationToken cancellationToken = default)
    {
        var requestBody = new QueryPostRequestBody
        {
            Requests =
            [
                new()
                    {
                        EntityTypes = entityTypes.ToList(),
                        Query = new SearchQuery { QueryString = query },
                        From = from,
                        Size = size,
                    }
            ],
        };

        var result = await graphServiceClient.Search.Query
            .PostAsQueryPostResponseAsync(requestBody, cancellationToken: cancellationToken);

        var searchItems = result?.Value?
            .SelectMany(y => y.HitsContainers ?? [])
            .SelectMany(y => y.Hits ?? [])
            .ToList();

        var hitContainer = result?.Value?.FirstOrDefault()?.HitsContainers?.FirstOrDefault();

        return JsonSerializer.Serialize(new
        {
            hits = searchItems,
            hitContainer?.MoreResultsAvailable,
            hitContainer?.Total
        });
    }

    /// <summary>
    /// Runs <see cref="SearchContent"/> across multiple entity combinations with throttling.
    /// </summary>
    private static async Task<IReadOnlyList<string?>> ExecuteSearchAcrossEntities(
        this GraphServiceClient client,
        string query,
        IEnumerable<EntityType?[]> entityCombinations,
        int maxConcurrency,
        CancellationToken cancellationToken)
    {
        using var semaphore = new SemaphoreSlim(maxConcurrency);

        var tasks = entityCombinations.Select(async entityTypes =>
        {
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                return await client.SearchContent(query, entityTypes, cancellationToken: cancellationToken);
            }
            finally
            {
                semaphore.Release();
            }
        });

        return await Task.WhenAll(tasks);
    }

    [Description("Search Microsoft 365 content")]
    [McpServerTool(ReadOnly = true)]
    public static async Task<CallToolResponse> SharePoint_Search(
        [Description("Search query")] string query,
        IServiceProvider serviceProvider,
        IMcpServer mcpServer,
        CancellationToken cancellationToken = default)
    {
        var client = await serviceProvider.GetOboGraphClient(mcpServer);
        var samplingService = serviceProvider.GetRequiredService<SamplingService>();

        var entityCombinations = new List<EntityType?[]>
            {
                new EntityType?[] { EntityType.Message, EntityType.ChatMessage },
                new EntityType?[] { EntityType.DriveItem, EntityType.Site, EntityType.ListItem },
            };

        // ----------------- BASIC SEARCH -------------------------------------------------
        if (mcpServer.ClientCapabilities?.Sampling is null)
        {
            var basicResults = await client.ExecuteSearchAcrossEntities(
                query,
                entityCombinations,
                maxConcurrency: 2,
                cancellationToken);

            return new CallToolResponse
            {
                Content = basicResults
                    .Where(t => !string.IsNullOrEmpty(t))
                    .Select(a => a?.ToTextContent())
                    .OfType<Content>()
                    .ToList()
            };
        }

        // ----------------- ADVANCED SEARCH WITH SAMPLING -------------------------------
        var queryArgs = new Dictionary<string, JsonElement>
        {
            ["topic"] = JsonSerializer.SerializeToElement(query),
            ["numberOfQueries"] = JsonSerializer.SerializeToElement("5"),
        };
        var configs = serviceProvider.GetRequiredService<IReadOnlyList<ServerConfig>>();
        var config = configs.GetServerConfig(mcpServer);

        var querySampling = await samplingService.GetPromptSample<QueryList>(
            serviceProvider,
            mcpServer,
            "get-sharepoint-serp-queries",
            queryArgs,
            "o4-mini",
            0,
            cancellationToken);

        var queries = (querySampling?.Queries ?? Enumerable.Empty<string>())
            .Append(query)
            .Distinct()
            .ToList();

        var queryTasks = queries.Select(async q =>
        {
            var entityResults = await client.ExecuteSearchAcrossEntities(
                q,
                entityCombinations,
                maxConcurrency: 5,
                cancellationToken);

            return string.Join("###", entityResults);
        });

        var concatenatedResults = string.Join("\n\n", await Task.WhenAll(queryTasks));

        return await serviceProvider.ExtractWithFacts(
            mcpServer,
            concatenatedResults,
            "https://graph.microsoft.com/beta/search/query",
            query,
            5,
            cancellationToken);
    }
}

public class QueryList
{
    [JsonPropertyName("queries")]
    public IEnumerable<string> Queries { get; set; } = null!;
}