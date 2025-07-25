using System.Text.Json;
using MCPhappey.Common;
using MCPhappey.Common.Extensions;
using MCPhappey.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace MCPhappey.Simplicate.Extensions;

public static class SimplicateExtensions
{
    
    public static DateTime? ParseDate(this string? dateString)
    {
        if (string.IsNullOrWhiteSpace(dateString))
            return null;
        if (DateTime.TryParse(dateString, out var dt))
            return dt;
        // eventueel: custom parse logic als je een ISO-formaat of andere verwacht
        return null;
    }


    public static int? ParseInt(this string? intString)
    {
        if (string.IsNullOrWhiteSpace(intString))
            return null;
        if (int.TryParse(intString, out var dt))
            return dt;
        // eventueel: custom parse logic als je een ISO-formaat of andere verwacht
        return null;
    }


    public static decimal ToAmount(this decimal item) =>
         Math.Round(item, 2, MidpointRounding.AwayFromZero);


    public static async Task<SimplicateData<T>?> GetSimplicatePageAsync<T>(
        this DownloadService downloadService,
        IServiceProvider serviceProvider,
        IMcpServer mcpServer,
        string url,
        CancellationToken cancellationToken = default)
    {
        var page = await downloadService.ScrapeContentAsync(serviceProvider, mcpServer, url, cancellationToken);
        var stringContent = page?.FirstOrDefault()?.Contents?.ToString();

        if (string.IsNullOrWhiteSpace(stringContent))
            return null;

        return JsonSerializer.Deserialize<SimplicateData<T>>(stringContent);
    }

    public static async Task<SimplicateItemData<T>?> GetSimplicateItemAsync<T>(
        this DownloadService downloadService,
        IServiceProvider serviceProvider,
        IMcpServer mcpServer,
        string url,
        CancellationToken cancellationToken = default)
    {
        var page = await downloadService.ScrapeContentAsync(serviceProvider, mcpServer, url, cancellationToken);
        var stringContent = page?.FirstOrDefault()?.Contents?.ToString();

        if (string.IsNullOrWhiteSpace(stringContent))
            return null;

        return JsonSerializer.Deserialize<SimplicateItemData<T>>(stringContent);
    }


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

            var result = await downloadService.GetSimplicatePageAsync<T>(
                        serviceProvider, mcpServer, url, cancellationToken);

            if (result == null || result.Data == null)
                break;

            var uri = new Uri(url);
            var domain = uri.Host;
            var markdown =
                  $"<details><summary><a href=\"{url}\" target=\"blank\">{domain}</a></summary>\n\n```\n{JsonSerializer.Serialize(result)}\n```\n</details>";
            await requestContext.Server.SendMessageNotificationAsync(markdown, LoggingLevel.Debug);

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

    public static async Task<ContentBlock?> PostSimplicateItemAsync<T>(
          this IServiceProvider serviceProvider,
          string baseUrl, // e.g. "https://{subdomain}.simplicate.nl/api/v2/project/project"
          T item,
          RequestContext<CallToolRequestParams> requestContext,
          CancellationToken cancellationToken = default)
    {
        var scraper = serviceProvider.GetServices<IContentScraper>()
         .OfType<SimplicateScraper>().First();

        return await scraper.PostSimplicateItemAsync(
         serviceProvider,
         baseUrl,
         item,
         requestContext: requestContext,
         cancellationToken: cancellationToken
     );
    }

    public static async Task<ContentBlock?> PostSimplicateItemAsync<T>(
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
            serviceProvider, baseUrl, json, cancellationToken);

        if (LoggingLevel.Debug.ShouldLog(requestContext.Server.LoggingLevel))
        {
            await requestContext.Server.SendMessageNotificationAsync(
                $"<details><summary>RESPONSE</summary>\n\n```\n{JsonSerializer.Serialize(response,
                    ResourceExtensions.JsonSerializerOptions)}\n```\n</details>",
                LoggingLevel.Debug
            );
        }

        return response.ToJsonContentBlock($"{baseUrl}/{response?.Data.Id}");
    }
}
