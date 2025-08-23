using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using MCPhappey.Common.Extensions;
using MCPhappey.Core.Extensions;
using MCPhappey.Core.Services;
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
        int size = 20,
        CancellationToken cancellationToken = default)
    {
        var requestBody = new QueryPostRequestBody
        {
            Requests =
            [
                new()
                    {
                        EntityTypes = [.. entityTypes],
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
        }, ResourceExtensions.JsonSerializerOptions);
    }

    /// <summary>
    /// Runs <see cref="SearchContent"/> across multiple entity combinations with throttling.
    /// </summary>
    private static async Task<IReadOnlyList<string?>> ExecuteSearchAcrossEntities(
        this GraphServiceClient client,
        string query,
        IEnumerable<EntityType?[]> entityCombinations,
        int maxConcurrency,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        using var semaphore = new SemaphoreSlim(maxConcurrency);

        var tasks = entityCombinations.Select(async entityTypes =>
        {
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                return await client.SearchContent(query, entityTypes, size: pageSize, cancellationToken: cancellationToken);
            }
            finally
            {
                semaphore.Release();
            }
        });

        return await Task.WhenAll(tasks);
    }

    [Description("Search Microsoft 365 content and return raw Microsoft search results")]
    [McpServerTool(Name = "sharepoint_search", Title = "Search Microsoft 365 content raw",
        OpenWorld = false, ReadOnly = true)]
    public static async Task<CallToolResult> SharePoint_Search(
        [Description("Search query")] string query,
        IServiceProvider serviceProvider,
        RequestContext<CallToolRequestParams> requestContext,
        [Description("Page size")] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var mcpServer = requestContext.Server;
        using var client = await serviceProvider.GetOboGraphClient(mcpServer);

        var entityCombinations = new List<EntityType?[]>
    {
        new EntityType?[] { EntityType.Message, EntityType.ChatMessage },
        new EntityType?[] { EntityType.DriveItem, EntityType.ListItem },
        new EntityType?[] { EntityType.Site }
    };

        var results = await client.ExecuteSearchAcrossEntities(query, entityCombinations, 2, pageSize, cancellationToken);

        return new CallToolResult
        {
            Content = [.. results
                .Where(r => !string.IsNullOrEmpty(r))
                .Select(r => r!.ToTextContentBlock())]
        };
    }

    [Description("Read a Microsoft 365 search result")]
    [McpServerTool(Name = "sharepoint_read_search_result", Title = "Read a Microsoft 365 search result",
        OpenWorld = false, ReadOnly = true)]
    public static async Task<CallToolResult> SharePoint_ReadSearchResult(
        [Description("Url to the search result item")] string url,
        IServiceProvider serviceProvider,
        RequestContext<CallToolRequestParams> requestContext,
        CancellationToken cancellationToken = default)
    {
        var downloadService = serviceProvider.GetRequiredService<DownloadService>();
        var mcpServer = requestContext.Server;

        var content = await downloadService.ScrapeContentAsync(serviceProvider, mcpServer, url, cancellationToken);
        var text = string.Join("\n\n", content.Select(c => c.Contents.ToString()));

        return new CallToolResult
        {
            Content = [text.ToTextContentBlock()]
        };
    }

    [Description("Executes a prompt on Microsoft 365 search results")]
    [McpServerTool(Name = "sharepoint_prompt_search_results", Title = "Executes a prompt on Microsoft 365 search results",
        Destructive = false, Idempotent = true, OpenWorld = false, ReadOnly = true)]
    public static async Task<CallToolResult> SharePoint_PromptSearchResults(
        [Description("Prompt to execute on the search results")] string prompt,
        [Description("List of urls to the search result items")] string urlList,
        IServiceProvider serviceProvider,
        RequestContext<CallToolRequestParams> requestContext,
        CancellationToken cancellationToken = default)
    {
        var urls = urlList.Split(['\n', ',', ';'], StringSplitOptions.RemoveEmptyEntries)
                          .Select(u => u.Trim()).Where(u => u.StartsWith("http"));

        var downloadService = serviceProvider.GetRequiredService<DownloadService>();
        var mcpServer = requestContext.Server;

        var allContent = new List<string>();
        foreach (var url in urls)
        {
            var scraped = await downloadService.ScrapeContentAsync(serviceProvider, mcpServer, url, cancellationToken);
            allContent.AddRange(scraped.Select(c => c.Contents.ToString()));
        }

        var samplingService = serviceProvider.GetRequiredService<SamplingService>();

        var args = new Dictionary<string, JsonElement>()
        {
            ["facts"] = JsonSerializer.SerializeToElement(string.Join("\n\n", allContent)),
            ["question"] = JsonSerializer.SerializeToElement(prompt)
        };

        var result = await samplingService.GetPromptSample(
            serviceProvider, mcpServer, "extract-with-facts", args, "gpt-5-mini",
            metadata: new Dictionary<string, object>()
                {
                    {"openai", new {
                         reasoning = new {
                            effort = "medium"
                         }
                     } },

                },
            cancellationToken: cancellationToken
        );

        return new CallToolResult
        {
            Content = [result.Content]
        };
    }
}

public class QueryList
{
    [JsonPropertyName("queries")]
    public IEnumerable<string> Queries { get; set; } = null!;
}