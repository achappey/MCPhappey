using MCPhappey.Auth.Extensions;
using MCPhappey.Auth.Models;
using MCPhappey.Common;
using MCPhappey.Common.Models;
using MCPhappey.Scrapers.Extensions;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Server;

namespace MCPhappey.Scrapers.Generic;

public class OboClientScraper(IHttpClientFactory httpClientFactory, ServerConfig serverConfig,
    OAuthSettings oAuthSettings) : IContentScraper
{
    public bool SupportsHost(ServerConfig currentConfig, string host)
    {
        return currentConfig.Server.ServerInfo.Name == serverConfig.Server.ServerInfo.Name
            && serverConfig.Server.OBO?.Keys.Any(a => a == host || host.EndsWith(a)) == true;
    }

    public async Task<IEnumerable<FileItem>?> GetContentAsync(IMcpServer mcpServer, IServiceProvider serviceProvider,
         string url, CancellationToken cancellationToken = default)
    {
        var tokenService = serviceProvider.GetService<HeaderProvider>();

        if (string.IsNullOrEmpty(tokenService?.Bearer))
        {
            return null;
        }

        var uri = new Uri(url);

        var httpClient = await httpClientFactory.GetOboHttpClient(tokenService.Bearer, uri.Host,
                serverConfig.Server, oAuthSettings);

        using var result = await httpClient.GetAsync(url, cancellationToken);

        return [await result.ToFileItem(url, cancellationToken: cancellationToken)];
    }
}
