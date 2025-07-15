using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;
using MCPhappey.Auth.Extensions;
using MCPhappey.Common.Extensions;
using MCPhappey.Common.Models;
using MCPhappey.Core.Extensions;
using MCPhappey.Core.Services;
using MCPhappey.Servers.SQL.Repositories;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace MCPhappey.Servers.SQL.Tools;

public static partial class ModelContextEditor
{
    [Description("List MCP-server resources")]
    [McpServerTool(Name = "ModelContextEditor_ListServerResources", ReadOnly = true, OpenWorld = false)]
    public static async Task<CallToolResult> ModelContextEditor_ListServerResources(
          [Description("Name of the server")]
            string serverName,
      IServiceProvider serviceProvider,
      CancellationToken cancellationToken = default)
    {
        var serverRepository = serviceProvider.GetRequiredService<ServerRepository>();
        var userId = serviceProvider.GetUserId();
        if (userId == null)
        {
            return "No user found".ToErrorCallToolResponse();
        }

        var server = await serverRepository.GetServer(serverName, cancellationToken);
        if (server?.Owners.Any(a => a.Id == userId) != true)
        {
            throw new UnauthorizedAccessException();
        }

        return JsonSerializer.Serialize(server.Resources
            .Select(a => new { a.Uri, a.Name, a.Description }))
            .ToTextCallToolResponse();
    }

    [Description("Adds a resource to a MCP-server")]
    [McpServerTool(Name = "ModelContextEditor_AddResource", OpenWorld = false)]
    public static async Task<CallToolResult> ModelContextEditor_AddResource(
        [Description("Name of the server")]
            string serverName,
        IServiceProvider serviceProvider,
        RequestContext<CallToolRequestParams> requestContext,
        IMcpServer mcpServer,
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

            var dto = await requestContext.Server.GetElicitResponse<AddMcpResource>(cancellationToken);
            var config = configs.GetServerConfig(mcpServer);

            var resource = await downloadService.ScrapeContentAsync(
                    serviceProvider,
                    mcpServer,
                    dto.Uri, cancellationToken);

            if (resource.Any())
            {
                var item = await serverRepository.AddServerResource(server.Id, dto.Uri, dto.Name, dto.Description);

                return JsonSerializer.Serialize(new { item.Uri, item.Name, item.Description, }).ToJsonCallToolResponse(dto.Uri);
            }

            return "No resource found".ToErrorCallToolResponse();
        }

        return "Access denied. Only owners can add resources".ToErrorCallToolResponse();
    }

    [Description("Updates a resource of a MCP-server")]
    [McpServerTool(Name = "ModelContextEditor_UpdateResource", OpenWorld = false)]
    public static async Task<CallToolResult> ModelContextEditor_UpdateResource(
    [Description("Name of the server")] string serverName,
    [Description("URI of the resource to update")] string resourceUri,
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

        var dto = await requestContext.Server.GetElicitResponse<UpdateMcpResource>(cancellationToken);

        var resource = server.Resources.FirstOrDefault(a => a.Uri == resourceUri) ?? throw new ArgumentNullException();
        resource.Name = dto.Name?.Trim() == "" ? "" : dto.Name!;
        resource.Description = dto.Description?.Trim() == "" ? "" : dto.Description;

        var updated = await serverRepository.UpdateResource(resource);

        return JsonSerializer.Serialize(updated).ToJsonCallToolResponse(resourceUri);
    }

    [Description("Deletes a resource from a MCP-server")]
    [McpServerTool(Name = "ModelContextEditor_DeleteResource", OpenWorld = false)]
    public static async Task<CallToolResult> ModelContextEditor_DeleteResource(
    [Description("Name of the server")] string serverName,
    [Description("URI of the resource to delete")] string resourceUri,
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

        if (dto.Uri?.Trim() != resourceUri.Trim())
            return $"Confirmation does not match URI '{resourceUri}'".ToErrorCallToolResponse();

        await serverRepository.DeleteResource(resourceUri);

        return $"Resource {resourceUri} has been deleted.".ToTextCallToolResponse();
    }

    [Description("Please confirm the URI of the resource you want to delete.")]
    public class ConfirmDeleteResource
    {
        [JsonPropertyName("uri")]
        [Required]
        [Description("Enter the exact URI of the resource to confirm deletion.")]
        public string Uri { get; set; } = default!;
    }


    [Description("Update one or more fields. Leave blank to skip updating that field. Use a single space to clear the value.")]
    public class UpdateMcpResource
    {
        [JsonPropertyName("name")]
        [Description("New name of the resource (optional).")]
        public string? Name { get; set; }

        [JsonPropertyName("description")]
        [Description("New description of the resource (optional).")]
        public string? Description { get; set; }
    }


    [Description("Please fill in the details to add a new resource to the specified MCP server.")]
    public class AddMcpResource
    {
        [JsonPropertyName("uri")]
        [Required]
        [Description("The URI of the resource to add.")]
        public string Uri { get; set; } = default!;

        [JsonPropertyName("name")]
        [Required]
        [Description("The name of the resource to add.")]
        public string Name { get; set; } = default!;

        [JsonPropertyName("description")]
        [Description("Optional description of the resource.")]
        public string? Description { get; set; }
    }
}

