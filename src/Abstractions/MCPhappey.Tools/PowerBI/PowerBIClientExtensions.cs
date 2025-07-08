using MCPhappey.Common.Models;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Server;
using MCPhappey.Auth.Models;
using MCPhappey.Auth.Extensions;
using MCPhappey.Common;
using Microsoft.PowerBI.Api;
using MCPhappey.Core.Extensions;

namespace MCPhappey.Tools.PowerBI;

public static class PowerBIClientExtensions
{
    public static async Task<PowerBIClient> GetOboPowerBIClient(this IServiceProvider serviceProvider,
    IMcpServer mcpServer)
    {
        var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
        var tokenService = serviceProvider.GetService<HeaderProvider>();
        var oAuthSettings = serviceProvider.GetService<OAuthSettings>();
        var server = serviceProvider.GetServerConfig(mcpServer);

        return await httpClientFactory.GetOboPowerBIClient(tokenService?.Bearer!, server?.Server!, oAuthSettings!);
    }

    public static async Task<PowerBIClient> GetOboPowerBIClient(this IHttpClientFactory httpClientFactory,
      string token,
      Server server,
      OAuthSettings oAuthSettings)
    {
        var delegated = await httpClientFactory.GetOboToken(token, "api.powerbi.com", server, oAuthSettings);
        var tokenCredentials = new Microsoft.Rest.TokenCredentials(delegated, "Bearer");
        return new PowerBIClient(new Uri("https://api.powerbi.com/"), tokenCredentials);
    }

}

