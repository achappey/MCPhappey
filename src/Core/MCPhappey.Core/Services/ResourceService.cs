using MCPhappey.Core.Extensions;
using MCPhappey.Common.Models;
using Microsoft.Extensions.Configuration;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace MCPhappey.Core.Services;

public class ResourceService(IConfiguration configuration,
    DownloadService downloadService)
{
    public async Task<ListResourceTemplatesResult> GetServerResourceTemplates(ServerConfig serverConfig) =>
       await Task.FromResult(serverConfig.ResourceTemplateList
                                ?? new());

    public async Task<ListResourcesResult> GetServerResources(ServerConfig serverConfig,
        CancellationToken cancellationToken = default)
    {
        switch (serverConfig.Server.ServerInfo.Name)
        {
            case "Agent2Agent":
                var url = configuration["Agent2AgentDiscovery"];

                return new()
                {
                    Resources = !string.IsNullOrEmpty(url) ? [new Resource()
                {
                    Uri = url!,
                    Name = "Discover other agents for collaboration through detailed agent cards"
                }] : []
                };
            /*    case "ModelContext-Servers":
                    var defaultItems = servers
                                 .FirstOrDefault(a => a.Server.ServerInfo.Name
                                     .Equals(serverConfig.Server.ServerInfo.Name, StringComparison.OrdinalIgnoreCase))?.ResourceList
                                 ?? new();

                    return new ListResourcesResult()
                    {
                        Resources =
                        [
                            .. defaultItems.Resources,
                            new Resource()
                            {
                                Uri = (httpContext.Request.IsHttps ? "https" : "http") + "://" + httpContext.Request.Host + "/servers",
                                Name = "Discover other Model Context servers for tools and resources"
                            }
                        ]
                    };*/

            default:
                return await Task.FromResult(serverConfig?.ResourceList
                                  ?? new());
        }
    }

    public async Task<ReadResourceResult> GetServerResource(IServiceProvider serviceProvider,
        IMcpServer mcpServer,
        ServerConfig serverConfig, string uri,
        CancellationToken cancellationToken = default)
    {
        var fileItem = await downloadService.ScrapeContentAsync(serviceProvider, mcpServer, uri,
                       cancellationToken);

        return fileItem.ToReadResourceResult();
    }
}
