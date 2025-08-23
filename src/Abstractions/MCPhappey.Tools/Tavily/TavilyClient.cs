using System.Text;
using System.Text.Json;

namespace MCPhappey.Tools.Tavily;

public interface ITavilyClient
{
    Task<string> SearchAsync(
            string query,
            int? maxResults = 5,
            string? startDate = null,
            string? endDate = null,
            bool? includeImages = false,
            bool? includeImageDescriptions = false,
            CancellationToken ct = default);

    Task<string> ExtractAsync(
        IEnumerable<string> urls,
        bool? includeImages = false,
        CancellationToken ct = default);

    Task<string> CrawlAsync(
        string url,
        CancellationToken ct = default);

    Task<string> MapAsync(
        string url,
        CancellationToken ct = default);
}

public sealed class TavilyClient(IHttpClientFactory httpClientFactory, string apiKey) : ITavilyClient
{
    private readonly HttpClient _http = httpClientFactory.CreateClient();
    private readonly string _apiKey = apiKey;

    public async Task<string> SearchAsync(string query, int? maxResults = 5,
        string? startDate = null, string? endDate = null,
        bool? includeImages = false,
        bool? includeImageDescriptions = false,
        CancellationToken ct = default)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, "https://api.tavily.com/search")
        {
            Content = new StringContent(
                JsonSerializer.Serialize(new
                {
                    query,
                    include_images = includeImages,
                    include_image_descriptions = includeImageDescriptions,
                    start_date = startDate,
                    end_date = endDate,
                    max_results = maxResults
                }),
                Encoding.UTF8, "application/json")
        };

        // Set/refresh Bearer header each call (in case key rotates)
        req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiKey);

        using var resp = await _http.SendAsync(req, ct);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadAsStringAsync(ct);
    }

    public async Task<string> ExtractAsync(IEnumerable<string> urls,
      bool? includeImages = false,
      CancellationToken ct = default)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, "https://api.tavily.com/extract")
        {
            Content = new StringContent(
                JsonSerializer.Serialize(new
                {
                    urls,
                    include_images = includeImages,
                }),
                Encoding.UTF8, "application/json")
        };

        // Set/refresh Bearer header each call (in case key rotates)
        req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiKey);

        using var resp = await _http.SendAsync(req, ct);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadAsStringAsync(ct);
    }

    public async Task<string> CrawlAsync(string url,
    CancellationToken ct = default)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, "https://api.tavily.com/crawl")
        {
            Content = new StringContent(
                JsonSerializer.Serialize(new
                {
                    url
                }),
                Encoding.UTF8, "application/json")
        };

        // Set/refresh Bearer header each call (in case key rotates)
        req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiKey);

        using var resp = await _http.SendAsync(req, ct);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadAsStringAsync(ct);
    }

    public async Task<string> MapAsync(string url,
        CancellationToken ct = default)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, "https://api.tavily.com/map")
        {
            Content = new StringContent(
                JsonSerializer.Serialize(new
                {
                    url
                }),
                Encoding.UTF8, "application/json")
        };

        // Set/refresh Bearer header each call (in case key rotates)
        req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiKey);

        using var resp = await _http.SendAsync(req, ct);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadAsStringAsync(ct);
    }
}
