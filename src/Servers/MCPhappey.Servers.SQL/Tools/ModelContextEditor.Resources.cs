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

        var notAccepted = dto?.NotAccepted();
        if (notAccepted != null) return notAccepted;
        var typed = dto?.GetTypedResult<AddMcpResource>() ?? throw new Exception();

        var resource = await downloadService.ScrapeContentAsync(
                serviceProvider,
                requestContext.Server,
                typed.Uri, cancellationToken);

        if (resource.Any())
        {
            var item = await serverRepository.AddServerResource(server.Id, typed.Uri, typed.Name, typed.Description);

            return JsonSerializer.Serialize(item.ToResource())
                .ToJsonCallToolResponse(typed.Uri);
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

        var notAccepted = dto?.NotAccepted();
        if (notAccepted != null) return notAccepted;
        var typed = dto?.GetTypedResult<UpdateMcpResource>() ?? throw new Exception();

        if (!string.IsNullOrEmpty(typed.Uri))
        {
            resource.Uri = typed.Uri;
        }

        if (!string.IsNullOrEmpty(typed.Description))
        {
            resource.Description = typed.Description;
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
        var notAccepted = dto?.NotAccepted();
        if (notAccepted != null) return notAccepted;
        var typed = dto?.GetTypedResult<ConfirmDeleteResource>() ?? throw new Exception();

        if (typed.Name?.Trim() != resourceName.Trim())
            return $"Confirmation does not match name '{resourceName}'".ToErrorCallToolResponse();

        var resource = server.Resources.FirstOrDefault(a => a.Name == resourceName) ?? throw new ArgumentNullException();
        await serverRepository.DeleteResource(resource.Id);

        return $"Resource {resourceName} has been deleted.".ToTextCallToolResponse();
    }
}

