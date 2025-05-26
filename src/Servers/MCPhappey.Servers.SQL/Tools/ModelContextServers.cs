using System.ComponentModel;
using System.Text.Json;
using MCPhappey.Auth.Extensions;
using MCPhappey.Common.Extensions;
using MCPhappey.Common.Models;
using MCPhappey.Core.Extensions;
using MCPhappey.Core.Services;
using MCPhappey.Servers.SQL.Models;
using MCPhappey.Servers.SQL.Repositories;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace MCPhappey.Servers.SQL.Tools;

public static class ModelContextServers
{
    [Description("Create a new MCP-server")]
    public static async Task<CallToolResponse> CreateServer(
        [Description("Name of the server")]
        string name,
        IServiceProvider serviceProvider,
        [Description("Server instructions")]
        string? instructions = null,
        CancellationToken cancellationToken = default)
    {
        var serverRepository = serviceProvider.GetRequiredService<ServerRepository>();
        var userId = serviceProvider.GetUserId();
        if (userId == null)
        {
            return "No user found".ToErrorCallToolResponse();
        }

        var server = await serverRepository.CreateServer(new Models.Server()
        {
            Name = name,
            Instructions = instructions,
            Owners = [new ServerOwner() {
                       Id = userId
                    }]
        }, cancellationToken);

        return JsonSerializer.Serialize(server).ToTextCallToolResponse();
    }

    [Description("Deletes a MCP-server")]
    public static async Task<CallToolResponse> DeleteServer(
        [Description("Name of the server to delete")]
        string name,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken = default)
    {
        var userId = serviceProvider.GetUserId();
        if (userId == null)
        {
            return "No user found".ToErrorCallToolResponse();
        }

        var serverRepository = serviceProvider.GetRequiredService<ServerRepository>();
        var server = await serverRepository.GetServer(name, cancellationToken);

        if (server?.Owners.Any(a => a.Id == userId) == true)
        {
            await serverRepository.DeleteServer(server.Id);

            return JsonSerializer.Serialize(server).ToTextCallToolResponse();
        }

        throw new UnauthorizedAccessException();
    }

    [Description("Adds a resource to a MCP-server")]
    public static async Task<CallToolResponse> AddResource(
        [Description("Name of the server")]
            string serverName,
        [Description("Uri of the resource to add")]
            string resourceUri,
        [Description("Name of the resource to add")]
            string resourceName,
        IServiceProvider serviceProvider,
        IMcpServer mcpServer,
        [Description("Description of the resource to add")]
            string? resourceDescription = null,
        CancellationToken cancellationToken = default)
    {
        var userId = serviceProvider.GetUserId();

        if (userId == null)
        {
            return "No user found".ToErrorCallToolResponse();
        }

        var downloadService = serviceProvider.GetRequiredService<DownloadService>();

        var serverRepository = serviceProvider.GetRequiredService<ServerRepository>();
        var server = await serverRepository.GetServer(serverName, cancellationToken);

        if (server?.Owners.Any(a => a.Id == userId) == true)
        {
            var configs = serviceProvider.GetRequiredService<IReadOnlyList<ServerConfig>>();
            var config = configs.GetServerConfig(mcpServer);

            var resource = await downloadService.ScrapeContentAsync(
                    serviceProvider, mcpServer,
                    resourceUri, cancellationToken);

            if (resource.Any())
            {
                var item = await serverRepository.AddServerResource(server.Id, resourceUri, resourceName, resourceDescription);

                return JsonSerializer.Serialize(item).ToJsonCallToolResponse(resourceUri);
            }

            return "No resource found".ToErrorCallToolResponse();
        }

        return "Access denied. Only owners can add resources".ToErrorCallToolResponse();
    }
}

