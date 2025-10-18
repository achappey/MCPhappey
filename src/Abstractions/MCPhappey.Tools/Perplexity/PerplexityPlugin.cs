using System.ComponentModel;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using MCPhappey.Core.Extensions;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace MCPhappey.Tools.Perplexity;

public static class PerplexityPlugin
{
    [Description("Get ranked search results from Perplexity’s continuously refreshed index with advanced filtering and customization options.")]
    [McpServerTool(Idempotent = false, OpenWorld = true,
        Destructive = false, ReadOnly = true,
        Title = "Perplexity AI search")]
    public static async Task<CallToolResult?> Perplexity_Search(
      [Description("The search query or queries to execute. A search query. Can be a single query or a list of queries for multi-query search.")] string query,
      IServiceProvider serviceProvider,
      RequestContext<CallToolRequestParams> requestContext,
      [Description("The maximum number of search results to return.")] int maxResults = 10,
      [Description("Controls the maximum number of tokens retrieved from each webpage during search processing. Higher values provide more comprehensive content extraction but may increase processing time.")] int maxTokensPerPage = 1024,
      [Description("Country code to filter search results by geographic location (e.g., 'US', 'GB', 'DE').")] string? country = null,
      CancellationToken cancellationToken = default) => await requestContext.WithExceptionCheck(async () =>
        await requestContext.WithStructuredContent(async () =>
    {
        var httpClientFactory = serviceProvider.GetService<IHttpClientFactory>()
            ?? throw new InvalidOperationException("No IHttpClientFactory found in service provider");

        var settings = serviceProvider.GetService<PerplexitySettings>()
            ?? throw new InvalidOperationException("No PerplexitySettings found in service provider");

        var httpClient = httpClientFactory.CreateClient();

        var url = $"https://api.perplexity.ai/search";

        // Prepare the HTTP POST request
        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(new
            {
                query,
                max_results = maxResults,
                max_tokens_per_page = maxTokensPerPage,
                country
            })
        };

        request.Headers.Add("Authorization", $"Bearer {settings.ApiKey}");

        using var response = await httpClient.SendAsync(request,
            HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<PerplexitySearchResults>(cancellationToken);
    }));

    public class PerplexitySearchResults
    {
        [JsonPropertyName("results")]
        public IEnumerable<PerplexitySearchResult> Results { get; set; } = [];
    }

    public class PerplexitySearchResult
    {
        [JsonPropertyName("title")]
        public string Title { get; set; } = null!;

        [JsonPropertyName("url")]
        public string Url { get; set; } = null!;

        [JsonPropertyName("date")]
        public string Date { get; set; } = null!;

        [JsonPropertyName("snippet")]
        public string Snippet { get; set; } = null!;

        [JsonPropertyName("last_update")]
        public string LastUpdate { get; set; } = null!;
    }
}

public class PerplexitySettings
{
    public string ApiKey { get; set; } = default!;
}
