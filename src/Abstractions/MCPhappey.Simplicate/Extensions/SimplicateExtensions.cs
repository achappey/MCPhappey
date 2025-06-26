using System.Text.Json;
using MCPhappey.Common.Extensions;
using MCPhappey.Core.Services;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace MCPhappey.Simplicate.Extensions;

//
// Summary:
//     ASP-NET Core helper to register Simplicate content-scraper plus the
//     required Azure Key Vault SecretClient.
//
public static class SimplicateExtensions
{
    public static decimal ToAmount(this decimal item) =>
         Math.Round(item, 2, MidpointRounding.AwayFromZero);

    public static async Task<List<T>> GetAllSimplicatePagesAsync<T>(
        this DownloadService downloadService,
        IServiceProvider serviceProvider,
        IMcpServer mcpServer,
        string baseUrl,
        string filterString,
        Func<int, string> progressTextSelector, 
        RequestContext<CallToolRequestParams> requestContext,
        int pageSize = 100,
        CancellationToken cancellationToken = default)
    {
        var results = new List<T>();
        int offset = 0;
        int? totalCount = null;
        int? totalPages = null;

        while (true)
        {
            int pageNumber = (offset / pageSize) + 1;
            string url = $"{baseUrl}?{filterString}&limit={pageSize}&offset={offset}&metadata=count";

            await requestContext.Server.SendProgressNotificationAsync(
                requestContext,
                pageNumber,
                progressTextSelector(pageNumber),
                totalPages > 0 ? totalPages : (int?)null,
                cancellationToken
            );

            var page = await downloadService.ScrapeContentAsync(serviceProvider, mcpServer, url, cancellationToken);
            var stringContent = page?.FirstOrDefault()?.Contents?.ToString();

            if (string.IsNullOrWhiteSpace(stringContent)) break;

            if (LoggingLevel.Debug.ShouldLog(requestContext.Server.LoggingLevel))
            {
                var uri = new Uri(url);
                var domain = uri.Host;
                var markdown =
                    $"<details><summary><a href=\"{url}\" target=\"blank\">{domain}</a></summary>\n\n```\n{stringContent}\n```\n</details>";

                await requestContext.Server.SendMessageNotificationAsync(markdown, LoggingLevel.Debug);
            }

            var result = JsonSerializer.Deserialize<SimplicateData<T>>(stringContent);
            if (result == null || result.Data == null) break;

            results.AddRange(result.Data);

            if (totalCount == null && result.Metadata != null)
            {
                totalCount = result.Metadata.Count;
                totalPages = (int)Math.Ceiling((double)totalCount.Value / pageSize);
            }

            offset += pageSize;
            if (totalCount.HasValue && offset >= totalCount.Value)
                break;
        }

        return results;
    }

    public static async Task<SimplicateNewItemData?> PostSimplicateItemAsync<T>(
        this SimplicateScraper downloadService,
        IServiceProvider serviceProvider,
        string baseUrl, // e.g. "https://{subdomain}.simplicate.nl/api/v2/project/project"
        T item,
        RequestContext<CallToolRequestParams> requestContext,
        CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(item, JsonSerializerOptions.Web);

        if (LoggingLevel.Debug.ShouldLog(requestContext.Server.LoggingLevel))
        {
            await requestContext.Server.SendMessageNotificationAsync(
                $"<details><summary>POST <code>{baseUrl}</code></summary>\n\n```\n{json}\n```\n</details>",
                LoggingLevel.Debug
            );
        }

        // Use your DownloadService to POST (assumes similar signature to ScrapeContentAsync)
        var response = await downloadService.PostContentAsync<T>(
            requestContext.Server, serviceProvider, baseUrl, json, cancellationToken);

        if (LoggingLevel.Debug.ShouldLog(requestContext.Server.LoggingLevel))
        {
            await requestContext.Server.SendMessageNotificationAsync(
                $"<details><summary>RESPONSE</summary>\n\n```\n{JsonSerializer.Serialize(response, JsonSerializerOptions.Web)}\n```\n</details>",
                LoggingLevel.Debug
            );
        }

        return response;
    }
}
