using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;
using MCPhappey.Auth.Extensions;
using MCPhappey.Common.Extensions;
using MCPhappey.Servers.SQL.Repositories;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace MCPhappey.Servers.SQL.Tools;

public static partial class ModelContextSecurityEditor
{
    [Description("List MCP-servers where the current user is owner of")]
    [McpServerTool(Name = "ModelContextSecurityEditor_ListServers", ReadOnly = true, OpenWorld = false)]
    public static async Task<CallToolResult> ModelContextSecurityEditor_ListServers(
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
        var userId = serviceProvider.GetUserId();
        if (userId == null)
        {
            return "No user found".ToErrorCallToolResponse();
        }

        var server = await serverRepository.GetServer(serverName, cancellationToken);

        if (server?.Owners.Any(a => a.Id == userId) != true)
            return "Access denied.".ToErrorCallToolResponse();

        var dto = await requestContext.Server.GetElicitResponse<McpServerOwner>(cancellationToken);

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
        var userId = serviceProvider.GetUserId();
        if (userId == null)
        {
            return "No user found".ToErrorCallToolResponse();
        }

        var server = await serverRepository.GetServer(serverName, cancellationToken);

        if (server?.Owners.Any(a => a.Id == userId) != true)
            return "Access denied.".ToErrorCallToolResponse();

        if (server.Owners.Count() <= 1)
            return "MCP servers need at least 1 owner.".ToErrorCallToolResponse();

        if (server.Owners.Any(a => a.Id == ownerUserId) != true)
        {
            return $"User {ownerUserId} is not an owner on server {serverName}.".ToErrorCallToolResponse();
        }

        var dto = await requestContext.Server.GetElicitResponse<McpServerOwner>(cancellationToken);

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
        var userId = serviceProvider.GetUserId();
        if (userId == null) return "No user found".ToErrorCallToolResponse();

        var serverRepository = serviceProvider.GetRequiredService<ServerRepository>();
        var server = await serverRepository.GetServer(serverName, cancellationToken);

        if (server?.Owners.Any(a => a.Id == userId) != true)
            return "Access denied.".ToErrorCallToolResponse();

        var dto = await requestContext.Server.GetElicitResponse<UpdateMcpServer>(cancellationToken);
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
        var userId = serviceProvider.GetUserId();
        if (userId == null) return "No user found".ToErrorCallToolResponse();

        var serverRepository = serviceProvider.GetRequiredService<ServerRepository>();
        var server = await serverRepository.GetServer(serverName, cancellationToken);

        if (server?.Owners.Any(a => a.Id == userId) != true)
            return "Access denied.".ToErrorCallToolResponse();

        var dto = await requestContext.Server.GetElicitResponse<McpSecurityGroup>(cancellationToken);

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
        var userId = serviceProvider.GetUserId();
        if (userId == null) return "No user found".ToErrorCallToolResponse();

        var serverRepository = serviceProvider.GetRequiredService<ServerRepository>();
        var server = await serverRepository.GetServer(serverName, cancellationToken);

        if (server?.Owners.Any(a => a.Id == userId) != true)
            return "Access denied.".ToErrorCallToolResponse();

        var dto = await requestContext.Server.GetElicitResponse<McpSecurityGroup>(cancellationToken);

        if (!server.Groups.Any(g => g.Id == dto.GroupId))
            return $"Group {dto.GroupId} not assigned.".ToErrorCallToolResponse();

        if (!dto.GroupId.Equals(securityGroupId, StringComparison.OrdinalIgnoreCase))
        {
            return $"Group {dto.GroupId} does not match {securityGroupId}.".ToErrorCallToolResponse();
        }

        await serverRepository.DeleteServerGroup(server.Id, dto.GroupId);

        return $"Security group {dto.GroupId} removed from MCP server {serverName}".ToTextCallToolResponse();
    }

    [Description("Please fill in the security group details.")]
    public class McpSecurityGroup
    {
        [JsonPropertyName("groupId")]
        [Required]
        [Description("The object ID of the security group.")]
        public string GroupId { get; set; } = default!;
    }

    [Description("Please fill in the MCP Server owner details.")]
    public class McpServerOwner
    {
        [JsonPropertyName("userId")]
        [Required]
        [Description("The user id of the MCP server owner.")]
        public string UserId { get; set; } = default!;
    }

    [Description("Update one or more fields. Leave blank to skip updating that field. Use a single space to clear the value.")]
    public class UpdateMcpServer
    {
        [JsonPropertyName("secured")]
        [Description("Enable if you would like to secure the MCP server.")]
        public bool? Secured { get; set; }
    }
}

