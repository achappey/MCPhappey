using System.ComponentModel;
using System.Text.Json;
using MCPhappey.Auth.Extensions;
using MCPhappey.Common.Extensions;
using MCPhappey.Servers.SQL.Extensions;
using MCPhappey.Servers.SQL.Models;
using MCPhappey.Servers.SQL.Repositories;
using MCPhappey.Servers.SQL.Tools.Models;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace MCPhappey.Servers.SQL.Tools;

public static partial class ModelContextEditor
{
    [Description("Create a new MCP-server")]
    [McpServerTool(Name = "ModelContextEditor_CreateServer", OpenWorld = false)]
    public static async Task<CallToolResult> ModelContextEditor_CreateServer(
        [Description("Name of the new server")]
        string serverName,
        IServiceProvider serviceProvider,
        RequestContext<CallToolRequestParams> requestContext,
        [Description("Instructions of thew new server")]
        string? instructions = null,
        CancellationToken cancellationToken = default)
    {
        var serverRepository = serviceProvider.GetRequiredService<ServerRepository>();
        var userId = serviceProvider.GetUserId();
        if (userId == null)
        {
            return "No user found".ToErrorCallToolResponse();
        }

        var dto = await requestContext.Server.GetElicitResponse(new NewMcpServer()
        {
            Name = serverName,
            Instructions = instructions
        }, cancellationToken);

        var server = await serverRepository.CreateServer(new Server()
        {
            Name = dto.Name.Slugify(),
            Instructions = dto.Instructions,
            Secured = true,
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
        var serverRepository = serviceProvider.GetRequiredService<ServerRepository>();
        var server = await serviceProvider.GetServer(serverName, cancellationToken);
        var dto = await requestContext.Server.GetElicitResponse<UpdateMcpServer>(cancellationToken);

        if (!string.IsNullOrEmpty(dto.Name))
        {
            server.Name = dto.Name.Slugify();
        }

        if (!string.IsNullOrEmpty(dto.Instructions))
        {
            server.Instructions = dto.Instructions;
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
        [Description("Name of the server")] string serverName,
        RequestContext<CallToolRequestParams> requestContext,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken = default)
    {
        var dto = await requestContext.Server.GetElicitResponse<DeleteMcpServer>(cancellationToken);

        if (dto.Name?.Trim() != serverName.Trim())
            return $"Confirmation does not match name '{serverName}'".ToErrorCallToolResponse();

        var serverRepository = serviceProvider.GetRequiredService<ServerRepository>();
        var server = await serviceProvider.GetServer(dto.Name, cancellationToken);

        await serverRepository.DeleteServer(server.Id);

        return "Server deleted".ToTextCallToolResponse();
    }
}

