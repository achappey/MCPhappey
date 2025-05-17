using System.Net.Mime;
using MCPhappey.Common.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Graph.Beta;
using Microsoft.Graph.Beta.Models;
using ModelContextProtocol.Server;
using MCPhappey.Auth.Models;
using MCPhappey.Auth.Extensions;
using MCPhappey.Common.Constants;
using ModelContextProtocol.Protocol;
using MCPhappey.Common;

namespace MCPhappey.Core.Extensions;

public static class GraphClientExtensions
{
    public static async Task<GraphServiceClient> GetOboGraphClient(this IServiceProvider serviceProvider,
      IMcpServer mcpServer)
    {
        var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
        var tokenService = serviceProvider.GetService<HeaderProvider>();
        var oAuthSettings = serviceProvider.GetService<OAuthSettings>();
        var server = serviceProvider.GetServerConfig(mcpServer);

        return await httpClientFactory.GetOboGraphClient(tokenService?.Bearer!, server?.Server!, oAuthSettings!);
    }

    public static async Task<GraphServiceClient> GetOboGraphClient(this IHttpClientFactory httpClientFactory,
        string token,
        Server server,
        OAuthSettings oAuthSettings)
    {
        var delegated = await httpClientFactory.GetOboToken(token, Hosts.MicrosoftGraph, server, oAuthSettings);

        var authProvider = new StaticTokenAuthProvider(delegated!);
        return new GraphServiceClient(authProvider);
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
}

