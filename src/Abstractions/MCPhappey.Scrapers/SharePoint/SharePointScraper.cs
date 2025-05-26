using MCPhappey.Auth.Models;
using MCPhappey.Common;
using MCPhappey.Common.Constants;
using MCPhappey.Common.Models;
using MCPhappey.Scrapers.Extensions;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Server;

namespace MCPhappey.Scrapers.SharePoint;

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
