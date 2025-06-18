using MCPhappey.Auth.Extensions;
using MCPhappey.Auth.Models;
using MCPhappey.Common;
using MCPhappey.Common.Constants;
using MCPhappey.Common.Models;
using MCPhappey.Scrapers.Extensions;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Server;

namespace MCPhappey.Scrapers.SharePoint;
/*
public class SharePointScraper(
    IHttpClientFactory httpClientFactory,
    ServerConfig serverConfig,
    OAuthSettings oAuthSettings) : IContentScraper
{
    public bool SupportsHost(ServerConfig currentConfig, string host)
        => host.EndsWith(".sharepoint.com", StringComparison.OrdinalIgnoreCase)
           && currentConfig?.Server.OBO?.ContainsKey(host) == true;

    public async Task<IEnumerable<FileItem>?> GetContentAsync(IMcpServer mcpServer, IServiceProvider serviceProvider,
         string url, CancellationToken cancellationToken = default)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return null;

        var tokenService = serviceProvider.GetService<HeaderProvider>();
        if (string.IsNullOrEmpty(tokenService?.Bearer))
            return null;

        // Detect Graph vs SharePoint REST based on URL path
        var isSharePointRest = uri.Host.EndsWith(".sharepoint.com", StringComparison.OrdinalIgnoreCase)
                            && uri.AbsolutePath.Contains("/_api/", StringComparison.OrdinalIgnoreCase);

        if (isSharePointRest)
        {
            // Use host-specific OBO token for SharePoint
            var sharepointHost = uri.Host;
            var httpClient = await httpClientFactory.GetOboHttpClient(
                tokenService.Bearer, sharepointHost, serverConfig.Server, oAuthSettings);

            using var result = await httpClient.GetAsync(url, cancellationToken);

            return [await result.ToFileItem(url, cancellationToken)];

            // return [await sharepointClient.GetAsync(url)];
        }
        else
        {
            // Default to Microsoft Graph
            var graphClient = await httpClientFactory.GetOboGraphClient(
                tokenService.Bearer, serverConfig.Server, oAuthSettings);

            return [await graphClient.GetFilesByUrl(url)];
        }
    }
}
*/

public class SharePointScraper(IHttpClientFactory httpClientFactory, ServerConfig serverConfig,
    OAuthSettings oAuthSettings) : IContentScraper
{
    public bool SupportsHost(ServerConfig currentConfig, string host)
        => host.EndsWith(".sharepoint.com", StringComparison.OrdinalIgnoreCase)
            && serverConfig.Server.OBO?.ContainsKey(Hosts.MicrosoftGraph) == true;

    public async Task<IEnumerable<FileItem>?> GetContentAsync(IMcpServer mcpServer, IServiceProvider serviceProvider,
         string url, CancellationToken cancellationToken = default)
    {
        var tokenService = serviceProvider.GetService<HeaderProvider>();

        if (string.IsNullOrEmpty(tokenService?.Bearer))
        {
            return null;
        }

        var graphClient = await httpClientFactory.GetOboGraphClient(tokenService.Bearer,
                serverConfig.Server, oAuthSettings);

        return [await graphClient.GetFilesByUrl(url)];
    }
}
