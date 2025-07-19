using System.ComponentModel;
using System.Text.Json;
using MCPhappey.Common.Extensions;
using MCPhappey.Servers.SQL.Extensions;
using MCPhappey.Servers.SQL.Repositories;
using MCPhappey.Servers.SQL.Tools.Models;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace MCPhappey.Servers.SQL.Tools;

public static partial class ModelContextSecurityEditor
{
    [Description("Adds an owner to a MCP-server")]
    [McpServerTool(Name = "ModelContextSecurityEditor_AddOwner", OpenWorld = false)]
    public static async Task<CallToolResult> ModelContextSecurityEditor_AddOwner(
       [Description("Name of the server")] string serverName,
       [Description("User id of new owner")] string ownerUserId,
       IServiceProvider serviceProvider,
       RequestContext<CallToolRequestParams> requestContext,
       CancellationToken cancellationToken = default)
    {
        var serverRepository = serviceProvider.GetRequiredService<ServerRepository>();
        var server = await serviceProvider.GetServer(serverName, cancellationToken);
        var dto = await requestContext.Server.GetElicitResponse(new McpServerOwner()
        {
            UserId = ownerUserId
        }, cancellationToken);

        if (server.Owners.Any(a => a.Id == dto.UserId) == true)
        {
            return $"Owner {dto.UserId} already exists on server {serverName}.".ToErrorCallToolResponse();
        }

        if (!dto.UserId.Equals(ownerUserId, StringComparison.OrdinalIgnoreCase))
        {
            return $"Owner {dto.UserId} does not match {ownerUserId}.".ToErrorCallToolResponse();
        }

        await serverRepository.AddServerOwner(server.Id, dto.UserId);

        return $"Owner {dto.UserId} added to MCP server {serverName}".ToTextCallToolResponse();
    }

    [Description("Removes an owner from a MCP-server")]
    [McpServerTool(Name = "ModelContextSecurityEditor_RemoveOwner", OpenWorld = false)]
    public static async Task<CallToolResult> ModelContextSecurityEditor_RemoveOwner(
       [Description("Name of the server")] string serverName,
       [Description("User id of owner")] string ownerUserId,
       IServiceProvider serviceProvider,
       RequestContext<CallToolRequestParams> requestContext,
       CancellationToken cancellationToken = default)
    {
        var serverRepository = serviceProvider.GetRequiredService<ServerRepository>();
        var server = await serviceProvider.GetServer(serverName, cancellationToken);

        if (server.Owners.Count() <= 1)
            return "MCP servers need at least 1 owner.".ToErrorCallToolResponse();

        if (server.Owners.Any(a => a.Id == ownerUserId) != true)
        {
            return $"User {ownerUserId} is not an owner on server {serverName}.".ToErrorCallToolResponse();
        }

        var dto = await requestContext.Server.GetElicitResponse(new McpServerOwner()
        {
            UserId = ownerUserId
        }, cancellationToken);

        if (!dto.UserId.Equals(ownerUserId, StringComparison.OrdinalIgnoreCase))
        {
            return $"Owner {dto.UserId} does not match {ownerUserId}.".ToErrorCallToolResponse();
        }

        await serverRepository.DeleteServerOwner(server.Id, dto.UserId);

        return $"Owner {dto.UserId} deleted from MCP server {serverName}".ToTextCallToolResponse();
    }

    [Description("Updates the security of a MCP-server")]
    [McpServerTool(Name = "ModelContextSecurityEditor_UpdateServerSecurity", OpenWorld = false)]
    public static async Task<CallToolResult> ModelContextSecurityEditor_UpdateServerSecurity(
      [Description("Name of the server")] string serverName,
      IServiceProvider serviceProvider,
      RequestContext<CallToolRequestParams> requestContext,
      CancellationToken cancellationToken = default)
    {
        var serverRepository = serviceProvider.GetRequiredService<ServerRepository>();
        var server = await serviceProvider.GetServer(serverName, cancellationToken);
        var dto = await requestContext.Server.GetElicitResponse(new UpdateMcpServerSecurity()
        {
            Secured = server.Secured
        }, cancellationToken);

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

    [Description("Adds a security group to a MCP-server")]
    [McpServerTool(Name = "ModelContextSecurityEditor_AddSecurityGroup", OpenWorld = false)]
    public static async Task<CallToolResult> ModelContextSecurityEditor_AddSecurityGroup(
        [Description("Name of the server")] string serverName,
        [Description("Entra id of security group to add")] string securityGroupId,
        IServiceProvider serviceProvider,
        RequestContext<CallToolRequestParams> requestContext,
        CancellationToken cancellationToken = default)
    {
        var serverRepository = serviceProvider.GetRequiredService<ServerRepository>();
        var server = await serviceProvider.GetServer(serverName, cancellationToken);
        var dto = await requestContext.Server.GetElicitResponse(new McpSecurityGroup()
        {
            GroupId = securityGroupId
        }, cancellationToken);

        if (server.Groups.Any(g => g.Id == dto.GroupId))
            return $"Group {dto.GroupId} already assigned.".ToErrorCallToolResponse();

        if (!dto.GroupId.Equals(securityGroupId, StringComparison.OrdinalIgnoreCase))
        {
            return $"Group {dto.GroupId} does not match {securityGroupId}.".ToErrorCallToolResponse();
        }

        await serverRepository.AddServerGroup(server.Id, dto.GroupId);

        return $"Security group {dto.GroupId} added to MCP server {serverName}".ToTextCallToolResponse();
    }

    [Description("Removes a security group from a MCP-server")]
    [McpServerTool(Name = "ModelContextSecurityEditor_RemoveSecurityGroup", OpenWorld = false)]
    public static async Task<CallToolResult> ModelContextSecurityEditor_RemoveSecurityGroup(
        [Description("Name of the server")] string serverName,
        [Description("Entra id of security group to remove")] string securityGroupId,
        IServiceProvider serviceProvider,
        RequestContext<CallToolRequestParams> requestContext,
        CancellationToken cancellationToken = default)
    {
        var serverRepository = serviceProvider.GetRequiredService<ServerRepository>();
        var server = await serviceProvider.GetServer(serverName, cancellationToken);
        var dto = await requestContext.Server.GetElicitResponse(new McpSecurityGroup()
        {
            GroupId = securityGroupId
        }, cancellationToken);

        if (!server.Groups.Any(g => g.Id == dto.GroupId))
            return $"Group {dto.GroupId} not assigned.".ToErrorCallToolResponse();

        if (!dto.GroupId.Equals(securityGroupId, StringComparison.OrdinalIgnoreCase))
        {
            return $"Group {dto.GroupId} does not match {securityGroupId}.".ToErrorCallToolResponse();
        }

        await serverRepository.DeleteServerGroup(server.Id, dto.GroupId);

        return $"Security group {dto.GroupId} removed from MCP server {serverName}".ToTextCallToolResponse();
    }
}

