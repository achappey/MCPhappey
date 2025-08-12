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
    [McpServerTool(
        Title = "Add an owner to an MCP-server",
        OpenWorld = false)]
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

        var notAccepted = dto?.NotAccepted();
        if (notAccepted != null) return notAccepted;
        var typed = dto?.GetTypedResult<McpServerOwner>() ?? throw new Exception();

        if (server.Owners.Any(a => a.Id == typed.UserId) == true)
        {
            return $"Owner {typed.UserId} already exists on server {serverName}.".ToErrorCallToolResponse();
        }

        if (!typed.UserId.Equals(ownerUserId, StringComparison.OrdinalIgnoreCase))
        {
            return $"Owner {typed.UserId} does not match {ownerUserId}.".ToErrorCallToolResponse();
        }

        await serverRepository.AddServerOwner(server.Id, typed.UserId);

        return $"Owner {typed.UserId} added to MCP server {serverName}".ToTextCallToolResponse();
    }

    [Description("Removes an owner from a MCP-server")]
    [McpServerTool(
        Title = "Remove an owner from an MCP-server",
        OpenWorld = false)]
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
        var notAccepted = dto?.NotAccepted();
        if (notAccepted != null) return notAccepted;
        var typed = dto?.GetTypedResult<McpServerOwner>() ?? throw new Exception();

        if (!typed.UserId.Equals(ownerUserId, StringComparison.OrdinalIgnoreCase))
        {
            return $"Owner {typed.UserId} does not match {ownerUserId}.".ToErrorCallToolResponse();
        }

        await serverRepository.DeleteServerOwner(server.Id, typed.UserId);

        return $"Owner {typed.UserId} deleted from MCP server {serverName}".ToTextCallToolResponse();
    }

    [Description("Updates the security of a MCP-server")]
    [McpServerTool(
        Title = "Update the security of an MCP-server",
        OpenWorld = false)]
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
        var notAccepted = dto?.NotAccepted();
        if (notAccepted != null) return notAccepted;
        var typed = dto?.GetTypedResult<UpdateMcpServerSecurity>() ?? throw new Exception();

        if (typed.Secured.HasValue)
        {
            server.Secured = typed.Secured.Value;
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
    [McpServerTool(
        Title = "Add a security group to an MCP-server",
        OpenWorld = false)]
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
        var notAccepted = dto?.NotAccepted();
        if (notAccepted != null) return notAccepted;
        var typed = dto?.GetTypedResult<McpSecurityGroup>() ?? throw new Exception();

        if (server.Groups.Any(g => g.Id == typed.GroupId))
            return $"Group {typed.GroupId} already assigned.".ToErrorCallToolResponse();

        if (!typed.GroupId.Equals(securityGroupId, StringComparison.OrdinalIgnoreCase))
        {
            return $"Group {typed.GroupId} does not match {securityGroupId}.".ToErrorCallToolResponse();
        }

        await serverRepository.AddServerGroup(server.Id, typed.GroupId);

        return $"Security group {typed.GroupId} added to MCP server {serverName}".ToTextCallToolResponse();
    }

    [Description("Removes a security group from a MCP-server")]
    [McpServerTool(
        Title = "Remove a security group from an MCP-server",
        OpenWorld = false)]
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
        var notAccepted = dto?.NotAccepted();
        if (notAccepted != null) return notAccepted;

        var typed = dto?.GetTypedResult<McpSecurityGroup>() ?? throw new Exception();

        if (!server.Groups.Any(g => g.Id == typed.GroupId))
            return $"Group {typed.GroupId} not assigned.".ToErrorCallToolResponse();

        if (!typed.GroupId.Equals(securityGroupId, StringComparison.OrdinalIgnoreCase))
        {
            return $"Group {typed.GroupId} does not match {securityGroupId}.".ToErrorCallToolResponse();
        }

        await serverRepository.DeleteServerGroup(server.Id, typed.GroupId);

        return $"Security group {typed.GroupId} removed from MCP server {serverName}".ToTextCallToolResponse();
    }
}

