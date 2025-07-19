using System.ComponentModel;
using System.Text.Json;
using MCPhappey.Auth.Extensions;
using MCPhappey.Common.Extensions;
using MCPhappey.Core.Extensions;
using MCPhappey.Core.Services;
using MCPhappey.Servers.SQL.Extensions;
using MCPhappey.Servers.SQL.Repositories;
using MCPhappey.Servers.SQL.Tools.Models;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace MCPhappey.Servers.SQL.Tools;

public static partial class ModelContextEditor
{
    [Description("Adds a resource to a MCP-server")]
    [McpServerTool(Name = "ModelContextEditor_AddResource", OpenWorld = false)]
    public static async Task<CallToolResult> ModelContextEditor_AddResource(
        [Description("Name of the server")]
            string serverName,
        [Description("The URI of the resource to add.")]
            string uri,
        [Description("The name of the resource to add.")]
            string name,
        IServiceProvider serviceProvider,
        RequestContext<CallToolRequestParams> requestContext,
        [Description("Optional description of the resource.")]
        string? description = null,
        CancellationToken cancellationToken = default)
    {
        var downloadService = serviceProvider.GetRequiredService<DownloadService>();
        var serverRepository = serviceProvider.GetRequiredService<ServerRepository>();
        var server = await serviceProvider.GetServer(serverName, cancellationToken);
        var dto = await requestContext.Server.GetElicitResponse(new AddMcpResource()
        {
            Uri = uri,
            Name = name,
            Description = description
        }, cancellationToken);

        var resource = await downloadService.ScrapeContentAsync(
                serviceProvider,
                requestContext.Server,
                dto.Uri, cancellationToken);

        if (resource.Any())
        {
            var item = await serverRepository.AddServerResource(server.Id, dto.Uri, dto.Name, dto.Description);

            return JsonSerializer.Serialize(item.ToResource())
                .ToJsonCallToolResponse(dto.Uri);
        }

        return "No resource found".ToErrorCallToolResponse();
    }

    [Description("Updates a resource of a MCP-server")]
    [McpServerTool(Name = "ModelContextEditor_UpdateResource", OpenWorld = false)]
    public static async Task<CallToolResult> ModelContextEditor_UpdateResource(
        [Description("Name of the server")] string serverName,
        [Description("Name of the resource to update")] string resourceName,
        IServiceProvider serviceProvider,
        RequestContext<CallToolRequestParams> requestContext,
        CancellationToken cancellationToken = default)
    {
        var serverRepository = serviceProvider.GetRequiredService<ServerRepository>();
        var server = await serviceProvider.GetServer(serverName, cancellationToken);
        var dto = await requestContext.Server.GetElicitResponse<UpdateMcpResource>(cancellationToken);
        var resource = server.Resources.FirstOrDefault(a => a.Name == resourceName) ?? throw new ArgumentNullException();

        if (!string.IsNullOrEmpty(dto.Uri))
        {
            resource.Uri = dto.Uri;
        }

        if (!string.IsNullOrEmpty(dto.Description))
        {
            resource.Description = dto.Description;
        }

        var updated = await serverRepository.UpdateResource(resource);

        return JsonSerializer.Serialize(updated.ToResource())
            .ToTextCallToolResponse();
    }

    [Description("Deletes a resource from a MCP-server")]
    [McpServerTool(Name = "ModelContextEditor_DeleteResource", OpenWorld = false)]
    public static async Task<CallToolResult> ModelContextEditor_DeleteResource(
        [Description("Name of the server")] string serverName,
        [Description("Name of the resource to delete")] string resourceName,
        IServiceProvider serviceProvider,
        RequestContext<CallToolRequestParams> requestContext,
        CancellationToken cancellationToken = default)
    {
        var userId = serviceProvider.GetUserId();
        if (userId == null) return "No user found".ToErrorCallToolResponse();

        var serverRepository = serviceProvider.GetRequiredService<ServerRepository>();
        var server = await serverRepository.GetServer(serverName, cancellationToken);

        if (server?.Owners.Any(a => a.Id == userId) != true)
            return "Access denied.".ToErrorCallToolResponse();

        var dto = await requestContext.Server.GetElicitResponse<ConfirmDeleteResource>(cancellationToken);

        if (dto.Name?.Trim() != resourceName.Trim())
            return $"Confirmation does not match name '{resourceName}'".ToErrorCallToolResponse();

        var resource = server.Resources.FirstOrDefault(a => a.Name == resourceName) ?? throw new ArgumentNullException();
        await serverRepository.DeleteResource(resource.Id);

        return $"Resource {resourceName} has been deleted.".ToTextCallToolResponse();
    }
}

