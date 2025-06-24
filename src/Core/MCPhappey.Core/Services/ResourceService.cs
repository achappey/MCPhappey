using MCPhappey.Common.Extensions;
using MCPhappey.Common.Models;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace MCPhappey.Core.Services;

public class ResourceService(DownloadService downloadService)
{
    public async Task<ListResourceTemplatesResult> GetServerResourceTemplates(ServerConfig serverConfig) =>
       await Task.FromResult(serverConfig.ResourceTemplateList
                                ?? new());

    public async Task<ListResourcesResult> GetServerResources(ServerConfig serverConfig,
        CancellationToken cancellationToken = default)
    {
        switch (serverConfig.Server.ServerInfo.Name)
        {
            default:
                return await Task.FromResult(serverConfig?.ResourceList
                                  ?? new());
        }
    }

    public async Task<ReadResourceResult> GetServerResource(IServiceProvider serviceProvider,
        IMcpServer mcpServer,
        ServerConfig serverConfig,
        string uri,
        CancellationToken cancellationToken = default)
    {
        var fileItem = await downloadService.ScrapeContentAsync(serviceProvider, mcpServer, uri,
                       cancellationToken);

        return fileItem.ToReadResourceResult();
    }
}
