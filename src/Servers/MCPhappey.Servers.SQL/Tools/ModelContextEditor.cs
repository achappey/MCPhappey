using System.ComponentModel;
using System.Text.Json;
using DocumentFormat.OpenXml.Wordprocessing;
using MCPhappey.Auth.Extensions;
using MCPhappey.Common.Extensions;
using MCPhappey.Common.Models;
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
    [Description("Clone a MCP-server")]
    [McpServerTool(Name = "ModelContextEditor_CloneServer",
        Title = "Clone an MCP-server",
        OpenWorld = false)]
    public static async Task<CallToolResult> ModelContextEditor_CloneServer(
     [Description("Name of the server to clone")]
        string cloneServerName,
     IServiceProvider serviceProvider,
     RequestContext<CallToolRequestParams> requestContext,
     [Description("Name of the new server")]
        string? newServerName = null,
     CancellationToken cancellationToken = default)
    {
        var serverConfigs = serviceProvider.GetRequiredService<IReadOnlyList<ServerConfig>>();
        var sourceServerConfig = serverConfigs.FirstOrDefault(a => a.Server.ServerInfo.Name == cloneServerName);
        var allCustomServers = await serviceProvider.GetServers(cancellationToken);
        var customServer = allCustomServers.FirstOrDefault(a => a.Name == cloneServerName);
        var userId = serviceProvider.GetUserId();

        if (userId == null)
            return "No user found".ToErrorCallToolResponse();

        if (sourceServerConfig?.SourceType == ServerSourceType.Dynamic)
        {
            if (customServer?.Owners?.Select(a => a.Id).Contains(userId) != true)
            {
                return "Only editors can clone a server".ToErrorCallToolResponse();
            }
        }
        else if (sourceServerConfig == null)
        {
            if (customServer?.Owners?.Select(a => a.Id).Contains(userId) != true)
            {
                return "Only editors can clone a server".ToErrorCallToolResponse();
            }
        }

        var (typedResult, notAccepted) = await requestContext.Server.TryElicit(new CloneMcpServer
        {
            Name = newServerName ?? string.Empty,
        }, cancellationToken);

        if (notAccepted != null) return notAccepted;
        if (typedResult == null) return "Something went wrong".ToErrorCallToolResponse();

        var serverRepository = serviceProvider.GetRequiredService<ServerRepository>();

        // Helper: Standardize server object
        async Task<object?> GetSourceServer()
        {
            if (sourceServerConfig?.SourceType == ServerSourceType.Static)
            {
                var s = sourceServerConfig;
                return new
                {
                    s.Server.ServerInfo.Name,
                    s.Server.Instructions,
                    Secured = true,
                    Prompts = s.PromptList?.Prompts?.Select(p => new
                    {
                        p.Prompt,
                        p.Template.Name,
                        p.Template.Description,
                        Arguments = p.Template.Arguments?.Select(a => new SQL.Models.PromptArgument
                        {
                            Name = a.Name,
                            Description = a.Description,
                            Required = a.Required
                        }).ToList()
                    }).ToList(),
                    Resources = s.ResourceList?.Resources?.Select(r => new
                    {
                        r.Uri,
                        r.Name,
                        r.Description
                    }).ToList(),
                    ResourceTemplates = s.ResourceTemplateList?.ResourceTemplates?.Select(r => new
                    {
                        Uri = r.UriTemplate,
                        r.Name,
                        r.Description
                    }).ToList(),
                    Tools = s.ToolList?.ToList()
                };
            }
            else
            {
                var s = await serviceProvider.GetServer(cloneServerName, cancellationToken);
                return new
                {
                    s.Name,
                    s.Instructions,
                    s.Secured,
                    Prompts = s.Prompts?.Select(p => new
                    {
                        Prompt = p.PromptTemplate,
                        p.Name,
                        p.Description,
                        Arguments = p.Arguments?.Select(a => new SQL.Models.PromptArgument
                        {
                            Name = a.Name,
                            Description = a.Description,
                            Required = a.Required
                        }).ToList()
                    }).ToList(),
                    Resources = s.Resources?.Select(r => new
                    {
                        r.Uri,
                        r.Name,
                        r.Description
                    }).ToList(),
                    ResourceTemplates = s.ResourceTemplates?.Select(r => new
                    {
                        Uri = r.TemplateUri,
                        r.Name,
                        r.Description
                    }).ToList(),
                    Tools = s.Tools?.Select(t => t.Name).ToList()
                };
            }
        }

        // Main logic
        var source = await GetSourceServer();
        if (source == null)
            return "Source server not found".ToErrorCallToolResponse();

        // Dynamically resolve properties with dynamic
        dynamic src = source;

        var dbServer = await serverRepository.CreateServer(new SQL.Models.Server
        {
            Name = typedResult.Name.Slugify(),
            Instructions = src.Instructions,
            Secured = src.Secured,
            Owners = [new ServerOwner { Id = userId }]
        }, cancellationToken);

        // Helper functions to reduce repetition
        async Task AddPromptsAsync()
        {
            if (src.Prompts == null) return;
            foreach (var p in src.Prompts)
            {
                await serverRepository.AddServerPrompt(
                    dbServer.Id,
                    p.Prompt,
                    p.Name,
                    p.Description,
                    p.Arguments
                );
            }
        }
        async Task AddResourcesAsync()
        {
            if (src.Resources == null) return;
            foreach (var r in src.Resources)
            {
                await serverRepository.AddServerResource(
                    dbServer.Id,
                    r.Uri,
                    r.Name,
                    r.Description
                );
            }
        }

        async Task AddResourceTemplatesAsync()
        {
            if (src.ResourceTemplates == null) return;
            foreach (var r in src.ResourceTemplates)
            {
                await serverRepository.AddServerResourceTemplate(
                    dbServer.Id,
                    r.Uri,
                    r.Name,
                    r.Description
                );
            }
        }

        async Task AddToolsAsync()
        {
            if (src.Tools == null) return;
            foreach (var t in src.Tools)
            {
                await serverRepository.AddServerTool(
                    dbServer.Id,
                    t
                );
            }
        }

        // Perform all cloning
        await AddPromptsAsync();
        await AddResourcesAsync();
        await AddResourceTemplatesAsync();
        await AddToolsAsync();

        var fullServer = await serviceProvider.GetServer(typedResult.Name.Slugify(), cancellationToken);
        return JsonSerializer.Serialize(new
        {
            fullServer.Name,
            Owners = fullServer.Owners.Select(z => z.Id),
            fullServer.Secured,
            SecurityGroups = fullServer.Groups.Select(z => z.Id)
        })
        .ToJsonCallToolResponse($"mcp-editor://server/{fullServer.Name}");
    }

    [Description("Create a new MCP-server")]
    [McpServerTool(Name = "ModelContextEditor_CreateServer",
        Title = "Create a new MCP-server",
        OpenWorld = false)]
    public static async Task<CallToolResult> ModelContextEditor_CreateServer(
        [Description("Name of the new server")]
        string serverName,
        IServiceProvider serviceProvider,
        RequestContext<CallToolRequestParams> requestContext,
        [Description("Instructions of the new server")]
        string? instructions = null,
        CancellationToken cancellationToken = default)
    {
        var serverRepository = serviceProvider.GetRequiredService<ServerRepository>();
        var userId = serviceProvider.GetUserId();
        if (userId == null)
        {
            return "No user found".ToErrorCallToolResponse();
        }

        var (typedResult, notAccepted) = await requestContext.Server.TryElicit(new NewMcpServer()
        {
            Name = serverName,
            Instructions = instructions,
            Secured = true,
        }, cancellationToken);
        if (notAccepted != null) return notAccepted;
        if (typedResult == null) return "Something went wrong".ToErrorCallToolResponse();

        var server = await serverRepository.CreateServer(new SQL.Models.Server()
        {
            Name = typedResult.Name.Slugify(),
            Instructions = typedResult.Instructions,
            Secured = typedResult.Secured ?? true,
            Hidden = typedResult.Hidden,
            Owners = [new ServerOwner() {
                       Id = userId
                    }]
        }, cancellationToken);

        return JsonSerializer.Serialize(new
        {
            server.Name,
            Owners = server.Owners.Select(z => z.Id),
            server.Secured,
            server.Hidden,
            SecurityGroups = server.Groups.Select(z => z.Id)
        })
        .ToJsonCallToolResponse($"mcp-editor://server/{serverName}");
    }

    [Description("Updates a MCP-server")]
    [McpServerTool(Name = "ModelContextEditor_UpdateServer",
        Title = "Update an MCP-server",
        OpenWorld = false)]
    public static async Task<CallToolResult> ModelContextEditor_UpdateServer(
      [Description("Name of the server")] string serverName,
      IServiceProvider serviceProvider,
      RequestContext<CallToolRequestParams> requestContext,
        [Description("Updated instructions for the server")]
        string? instructions = null,
        [Description("If the server should be hidden")]
        bool? hidden = null,
      CancellationToken cancellationToken = default)
    {
        var server = await serviceProvider.GetServer(serverName, cancellationToken);
        var (typed, notAccepted) = await requestContext.Server.TryElicit(new UpdateMcpServer()
        {
            Name = serverName,
            Instructions = instructions ?? server.Instructions,
            Hidden = hidden ?? server.Hidden
        }, cancellationToken);
        if (notAccepted != null) return notAccepted;
        if (typed == null) return "Invalid response".ToErrorCallToolResponse();

        if (!string.IsNullOrEmpty(typed.Name))
        {
            server.Name = typed.Name.Slugify();
        }

        server.Instructions = typed.Instructions;
        server.Hidden = typed.Hidden;

        var serverRepository = serviceProvider.GetRequiredService<ServerRepository>();
        var updated = await serverRepository.UpdateServer(server);

        return JsonSerializer.Serialize(new
        {
            server.Name,
            Owners = server.Owners.Select(z => z.Id),
            server.Secured,
            SecurityGroups = server.Groups.Select(z => z.Id),
            server.Hidden
        })
        .ToJsonCallToolResponse($"mcp-editor://server/{serverName}");
    }

    [Description("Deletes a MCP-server")]
    [McpServerTool(Name = "ModelContextEditor_DeleteServer",
        Title = "Delete an MCP-server",
        OpenWorld = false)]
    public static async Task<CallToolResult> ModelContextEditor_DeleteServer(
        [Description("Name of the server")] string serverName,
        RequestContext<CallToolRequestParams> requestContext,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken = default)
    {
        var repo = serviceProvider.GetRequiredService<ServerRepository>();

        // One-liner again
        return await requestContext.ConfirmAndDeleteAsync<DeleteMcpServer>(
            serverName,
            async _ =>
            {
                var server = await serviceProvider.GetServer(serverName, cancellationToken);
                await repo.DeleteServer(server.Id);
            },
            "Server deleted.",
            cancellationToken);
    }
}

