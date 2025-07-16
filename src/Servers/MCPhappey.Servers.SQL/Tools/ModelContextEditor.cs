using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;
using MCPhappey.Auth.Extensions;
using MCPhappey.Common.Extensions;
using MCPhappey.Servers.SQL.Extensions;
using MCPhappey.Servers.SQL.Models;
using MCPhappey.Servers.SQL.Repositories;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace MCPhappey.Servers.SQL.Tools;

public static partial class ModelContextEditor
{
    [Description("List MCP-servers where the current user is owner of")]
    [McpServerTool(Name = "ModelContextEditor_ListServers", ReadOnly = true, OpenWorld = false)]
    public static async Task<CallToolResult> ModelContextEditor_ListServers(
       IServiceProvider serviceProvider,
       CancellationToken cancellationToken = default)
    {
        var serverRepository = serviceProvider.GetRequiredService<ServerRepository>();
        var userId = serviceProvider.GetUserId();
        if (userId == null)
        {
            return "No user found".ToErrorCallToolResponse();
        }

        var servers = await serverRepository.GetServers(cancellationToken);
        var userServers = servers.Where(a => a.Owners.Any(a => a.Id == userId)).Select(z => new
        {
            z.Name,
            Owners = z.Owners.Select(z => z.Id),
            z.Secured,
            SecurityGroups = z.Groups.Select(z => z.Id)
        });

        return JsonSerializer.Serialize(userServers).ToTextCallToolResponse();
    }

    [Description("Create a new MCP-server")]
    [McpServerTool(Name = "ModelContextEditor_CreateServer", OpenWorld = false)]
    public static async Task<CallToolResult> ModelContextEditor_CreateServer(
        IServiceProvider serviceProvider,
        RequestContext<CallToolRequestParams> requestContext,
        CancellationToken cancellationToken = default)
    {
        var serverRepository = serviceProvider.GetRequiredService<ServerRepository>();
        var userId = serviceProvider.GetUserId();
        if (userId == null)
        {
            return "No user found".ToErrorCallToolResponse();
        }

        var dto = await requestContext.Server.GetElicitResponse<NewMcpServer>(cancellationToken);
        var server = await serverRepository.CreateServer(new Models.Server()
        {
            Name = dto.Name.Slugify(),
            Instructions = dto.Instructions,
            Secured = dto.Secured ?? true,
            Owners = [new ServerOwner() {
                       Id = userId
                    }]
        }, cancellationToken);

        return JsonSerializer.Serialize(new
        {
            server.Name,
            Owners = server.Owners.Select(z => z.Id),
            server.Secured,
            SecurityGroups = server.Groups.Select(z => z.Id)
        }).ToTextCallToolResponse();
    }

    [Description("Updates a MCP-server")]
    [McpServerTool(Name = "ModelContextEditor_UpdateServer", OpenWorld = false)]
    public static async Task<CallToolResult> ModelContextEditor_UpdateServer(
      [Description("Name of the server")] string serverName,
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

        var dto = await requestContext.Server.GetElicitResponse<UpdateMcpServer>(cancellationToken);

        if (!string.IsNullOrEmpty(dto.Name))
        {
            server.Name = dto.Name.Slugify();
        }

        if (!string.IsNullOrEmpty(dto.Instructions))
        {
            server.Instructions = dto.Instructions;
        }

        if (dto.Secured.HasValue)
        {
            server.Secured = dto.Secured.Value;
        }

        var updated = await serverRepository.UpdateServer(server);

        return JsonSerializer.Serialize(new
        {
            server.Name,
            Owners = server.Owners.Select(z => z.Id),
            server.Secured,
            SecurityGroups = server.Groups.Select(z => z.Id)
        }).ToTextCallToolResponse();
    }

    [Description("Deletes a MCP-server")]
    [McpServerTool(Name = "ModelContextEditor_DeleteServer", OpenWorld = false)]
    public static async Task<CallToolResult> ModelContextEditor_DeleteServer(
      RequestContext<CallToolRequestParams> requestContext,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken = default)
    {
        var userId = serviceProvider.GetUserId();
        if (userId == null)
        {
            return "No user found".ToErrorCallToolResponse();
        }

        var dto = await requestContext.Server.GetElicitResponse<DeleteMcpServer>(cancellationToken);
        var serverRepository = serviceProvider.GetRequiredService<ServerRepository>();
        var server = await serverRepository.GetServer(dto.Name, cancellationToken);

        if (server?.Owners.Any(a => a.Id == userId) == true)
        {
            await serverRepository.DeleteServer(server.Id);

            return "Server deleted".ToTextCallToolResponse();
        }

        throw new UnauthorizedAccessException();
    }

    [Description("Please fill in the MCP Server details.")]
    public class NewMcpServer
    {
        [JsonPropertyName("name")]
        [Required]
        [Description("The MCP server name.")]
        public string Name { get; set; } = default!;

        [JsonPropertyName("instructions")]
        [Description("The MCP server instructions.")]
        public string? Instructions { get; set; }

        [JsonPropertyName("secured")]
        [DefaultValue(true)]
        [Description("Enable if you would like to secure the MCP server.")]
        public bool? Secured { get; set; }

    }

    [Description("Please fill in the MCP Server name to confirm deletion.")]
    public class DeleteMcpServer
    {
        [JsonPropertyName("name")]
        [Required]
        [Description("The MCP server name.")]
        public string Name { get; set; } = default!;
    }

    [Description("Update one or more fields. Leave blank to skip updating that field. Use a single space to clear the value.")]
    public class UpdateMcpServer
    {
        [JsonPropertyName("name")]
        [Description("New name of the resource (optional).")]
        public string? Name { get; set; }

        [JsonPropertyName("instructions")]
        [Description("The MCP server instructions.")]
        public string? Instructions { get; set; }

        [JsonPropertyName("secured")]
        [Description("Enable if you would like to secure the MCP server.")]
        public bool? Secured { get; set; }
    }
}

