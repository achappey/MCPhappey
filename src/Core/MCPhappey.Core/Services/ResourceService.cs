using MCPhappey.Common.Extensions;
using MCPhappey.Common.Models;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace MCPhappey.Core.Services;

public class ResourceService(DownloadService downloadService, IServerDataProvider dynamicDataService)
{
    public async Task<ListResourceTemplatesResult> GetServerResourceTemplates(ServerConfig serverConfig,
          CancellationToken cancellationToken = default) => serverConfig.SourceType switch
          {
              ServerSourceType.Static => await Task.FromResult(serverConfig?.ResourceTemplateList
                                                ?? new()),
              ServerSourceType.Dynamic => await dynamicDataService.GetResourceTemplatesAsync(serverConfig.Server.ServerInfo.Name, cancellationToken),
              _ => await Task.FromResult(serverConfig?.ResourceTemplateList
                                                ?? new()),
          };

    public async Task<ListResourcesResult> GetServerResources(ServerConfig serverConfig,
        CancellationToken cancellationToken = default) => serverConfig.SourceType switch
        {
            ServerSourceType.Static => await Task.FromResult(serverConfig?.ResourceList
                                              ?? new()),
            ServerSourceType.Dynamic => await dynamicDataService.GetResourcesAsync(serverConfig.Server.ServerInfo.Name, cancellationToken),
            _ => await Task.FromResult(serverConfig?.ResourceList
                                              ?? new()),
        };

    public async Task<ReadResourceResult> GetServerResource(IServiceProvider serviceProvider,
        McpServer mcpServer,
        string uri,
        CancellationToken cancellationToken = default)
    {
        var fileItem = await downloadService.ScrapeContentAsync(serviceProvider, mcpServer, uri,
                       cancellationToken);

        return fileItem.ToReadResourceResult();
    }
}
