using System.Text.Json;
using System.Web;
using Microsoft.Graph.Beta;
using Microsoft.Graph.Beta.Models;
using ModelContextProtocol.Protocol.Types;

namespace MCPhappey.Core.Extensions;

public static class SharePointNewsExtensions
{
    public static async Task<ReadResourceResult?> GetResourcesFromNewsPagesAsync(
     this GraphServiceClient graphClient,
     string fullUrl)
    {
        var uri = new Uri(fullUrl);
        var hostname = uri.Host;
        var queryParams = HttpUtility.ParseQueryString(uri.Query);

        var fullServerRelativeUrl = HttpUtility.UrlDecode(queryParams["serverRelativeUrl"]);
        var sitePath = fullServerRelativeUrl?.Split("/SitePages", StringSplitOptions.None)[0];
        var pagesListId = queryParams["pagesListId"];

        if (string.IsNullOrEmpty(sitePath) || string.IsNullOrEmpty(pagesListId))
            throw new ArgumentException("Missing site path or pages list ID.");

        // Get the site
        var site = await graphClient
            .Sites[$"{hostname}:{sitePath}"]
            .GetAsync();

        // Get the list items from Site Pages
        var listItems = await graphClient
            .Sites[site?.Id]
            .Lists[pagesListId]
            .Items
            .GetAsync((a) =>
            {
                a.QueryParameters.Expand = ["fields"];
                a.QueryParameters.Orderby = ["lastModifiedDateTime desc"];
            });

        // Extract title, description, webUrl from each item
        var pages = listItems?.Value?.Select(item =>
        {
            IDictionary<string, object> fields = item.Fields?.AdditionalData ?? new Dictionary<string, object>();

            fields.TryGetValue("Title", out var titleObj);
            fields.TryGetValue("Description", out var descObj);

            var title = titleObj?.ToString();
            var description = descObj?.ToString();

            return new
            {
                title,
                description,
                webUrl = item.WebUrl,
                lastModifiedDateTime = item.LastModifiedDateTime
            };
        });

        // Serialize to JSON
        var json = JsonSerializer.Serialize(pages);

        return json.ToJsonReadResourceResult(fullUrl);
    }

    public static async Task<IEnumerable<ListItem>?> GetNewsPagesFromUrlAsync(
        this GraphServiceClient graphClient,
        string fullUrl)
    {
        var uri = new Uri(fullUrl);
        var hostname = uri.Host;
        var queryParams = HttpUtility.ParseQueryString(uri.Query);

        var fullServerRelativeUrl = HttpUtility.UrlDecode(queryParams["serverRelativeUrl"]);
        var sitePath = fullServerRelativeUrl?.Split("/SitePages", StringSplitOptions.None)[0];

        var pagesListId = queryParams["pagesListId"];

        if (string.IsNullOrEmpty(sitePath) || string.IsNullOrEmpty(pagesListId))
        {
            throw new ArgumentException("Invalid SharePoint news URL: missing serverRelativeUrl or pagesListId.");
        }

        // Get the site
        var site = await graphClient
            .Sites[$"{hostname}:{sitePath}"]
            .GetAsync();

        // Get Site Pages
        var items = await graphClient
            .Sites[site?.Id]
            .Lists[pagesListId]
            .Items
            .GetAsync();

        return items?.Value;
    }
}

