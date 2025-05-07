using MCPhappey.Core.Extensions;
using MCPhappey.Core.Models.Protocol;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using ModelContextProtocol.Protocol.Types;

namespace MCPhappey.Core.Services;

public class ResourceService(IConfiguration configuration,
    DownloadService downloadService,
    IReadOnlyList<ServerConfig> servers)
{
    public async Task<ListResourceTemplatesResult> GetServerResourceTemplates(Server server) =>
       await Task.FromResult(servers
                                .FirstOrDefault(a => a.Server.ServerInfo.Name
                                    .Equals(server.ServerInfo.Name, StringComparison.OrdinalIgnoreCase))?.ResourceTemplateList
                                ?? new());
    public async Task<ListResourcesResult> GetServerResources(Server server,
        HttpContext httpContext, CancellationToken cancellationToken = default)
    {
        switch (server.ServerInfo.Name)
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
            case "ModelContext-Servers":
                var defaultItems = servers
                             .FirstOrDefault(a => a.Server.ServerInfo.Name
                                 .Equals(server.ServerInfo.Name, StringComparison.OrdinalIgnoreCase))?.ResourceList
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
                };

            default:
                return await Task.FromResult(servers
                                  .FirstOrDefault(a => a.Server.ServerInfo.Name
                                      .Equals(server.ServerInfo.Name, StringComparison.OrdinalIgnoreCase))?.ResourceList
                                  ?? new());
        }
    }

    public async Task<ReadResourceResult> GetServerResource(Server server, string uri,
       HttpContext httpContext, CancellationToken cancellationToken = default)
    {
        var fileItem = await downloadService.GetContentAsync(uri,
                       httpContext, cancellationToken);

        return fileItem.ToReadResourceResult();
    }
}
