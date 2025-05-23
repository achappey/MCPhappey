using MCPhappey.Common.Models;
using Microsoft.Graph.Beta;
using MCPhappey.Auth.Models;
using MCPhappey.Auth.Extensions;
using MCPhappey.Common.Constants;
using Microsoft.Graph.Beta.Models;
using System.Net.Mime;
using System.Text.Json;
using ModelContextProtocol.Protocol;
using MCPhappey.Common.Extensions;

namespace MCPhappey.Scrapers.Extensions;

public static class GraphClientExtensions
{
    public static async Task<GraphServiceClient> GetOboGraphClient(this IHttpClientFactory httpClientFactory,
        string token,
        Server server,
        OAuthSettings oAuthSettings)
    {
        var delegated = await httpClientFactory.GetOboToken(token, Hosts.MicrosoftGraph, server, oAuthSettings);

        var authProvider = new StaticTokenAuthProvider(delegated!);
        return new GraphServiceClient(authProvider);
    }

    public static async Task<FileItem> GetFilesByUrl(this GraphServiceClient graphServiceClient,
        string url)
    {
        var result = await graphServiceClient.GetDriveItem(url);

        if (result?.Folder != null)
        {
            return await graphServiceClient.GetFilesByFolder(result);
        }
        else
        {
            var content = await graphServiceClient.GetDriveItemContentAsync(result?.ParentReference?.DriveId!, result?.Id!);

            if (content != null)
            {
                return content;
            }
        }

        throw new Exception("Something went wrong. Only valid shareId or sharing URL are valid.");
    }

    public static Task<DriveItem?> GetDriveItem(this GraphServiceClient client, string link,
           CancellationToken cancellationToken = default)
    {
        string base64Value = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(link));
        string encodedUrl = "u!" + base64Value.TrimEnd('=').Replace('/', '_').Replace('+', '-');

        return client.Shares[encodedUrl].DriveItem.GetAsync(cancellationToken: cancellationToken);
    }

    public static Resource ToResource(this DriveItem driveItem) =>
           new()
           {
               Name = driveItem?.Name!,
               Uri = driveItem?.WebUrl!,
               Size = driveItem?.Size,
               Description = driveItem?.Description,
               MimeType = driveItem?.File?.MimeType ?? (driveItem?.Folder != null
                    ? MediaTypeNames.Application.Json : driveItem?.File?.MimeType)
           };

    private static async Task<FileItem> GetFilesByFolder(this GraphServiceClient graphServiceClient,
       DriveItem driveItem)
    {
        if (driveItem?.Folder != null)
        {
            var items = await graphServiceClient.Drives[driveItem?.ParentReference?.DriveId!].Items[driveItem?.Id!].Children.GetAsync();

            return JsonSerializer
                            .Serialize(items?.Value?.Select(t => t.ToResource()))
                            .ToJsonFileItem(driveItem?.WebUrl!);
        }

        throw new Exception("Only folders are supported");
    }

    public static async Task<FileItem> GetDriveItemContentAsync(this GraphServiceClient client, string driveId, string itemId)
    {
        var item = await client.Drives[driveId].Items[itemId].GetAsync();

        // Throw an exception if the item is not a file or ContentType is missing
        if (item?.File == null || item.File.MimeType == null)
            throw new InvalidOperationException("The item is not a file or MIME type unknown.");

        string contentType = item.File.MimeType;

        await using var stream = await client.Drives[driveId].Items[itemId].Content
            .GetAsync() ?? throw new InvalidOperationException("Stream cannot be null");

        var finalContentType = !string.IsNullOrEmpty(item.Name)
                      && Path
                      .GetExtension(item.Name)
                      .Equals(".csv", StringComparison.InvariantCultureIgnoreCase)
                        ? MediaTypeNames.Text.Csv : contentType;

        return new()
        {
            Contents = BinaryData.FromStream(stream),
            Uri = item?.WebUrl!,
            MimeType = finalContentType,
        };

    }
}

