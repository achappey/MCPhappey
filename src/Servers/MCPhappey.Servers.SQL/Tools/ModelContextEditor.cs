using System.ComponentModel;
using System.Text.Json;
using DocumentFormat.OpenXml.Wordprocessing;
using MCPhappey.Auth.Extensions;
using MCPhappey.Common.Extensions;
using MCPhappey.Common.Models;
using MCPhappey.Core.Extensions;
using MCPhappey.Servers.SQL.Extensions;
using MCPhappey.Servers.SQL.Models;
using MCPhappey.Servers.SQL.Repositories;
using MCPhappey.Servers.SQL.Tools.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace MCPhappey.Servers.SQL.Tools;

public static partial class ModelContextEditor
{
    [Description("Clone a MCP-server")]
    [McpServerTool(Title = "Clone a MCP-server",
        Destructive = false,
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

        var (typedResult, notAccepted, result) = await requestContext.Server.TryElicit(new CloneMcpServer
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
                    s.Server.ServerInfo.Title,
                    s.Server.ServerInfo.WebsiteUrl,
                    s.Server.ServerInfo.Description,
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
                    s.Title,
                    s.Description,
                    s.WebsiteUrl,
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
            Title = src.Title,
            Description = src.Description,
            WebsiteUrl = src.WebsiteUrl,
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
    [McpServerTool(Title = "Create a new MCP-server",
        OpenWorld = false)]
    public static async Task<CallToolResult?> ModelContextEditor_CreateServer(
        [Description("Name of the new server")]
        string serverName,
        [Description("Publicly visible description for the server. Used for discovery in the registry")]
        string serverDescription,
        IServiceProvider serviceProvider,
        RequestContext<CallToolRequestParams> requestContext,
        [Description("Optional title for the server")]
        string? serverTitle = null,
        [Description("Instructions of the new server")]
        string? instructions = null,
        [Description("Website url")]
        string? websiteUrl = null,
        CancellationToken cancellationToken = default) => await requestContext.WithExceptionCheck(async () =>
    {
        var serverRepository = serviceProvider.GetRequiredService<ServerRepository>();
        var userId = serviceProvider.GetUserId();
        if (userId == null) return "No user found".ToErrorCallToolResponse();

        var serverExists = await serviceProvider.ServerExists(serverName, cancellationToken);
        if (serverExists) return "Servername already in use".ToErrorCallToolResponse();

        var (typedResult, notAccepted, result) = await requestContext.Server.TryElicit(new NewMcpServer()
        {
            Name = serverName,
            WebsiteUrl = string.IsNullOrEmpty(websiteUrl) ? null : new Uri(websiteUrl),
            Title = serverTitle,
            Description = serverDescription,
            Instructions = instructions,
            Secured = true,
        }, cancellationToken);

        if (notAccepted != null) return notAccepted;
        if (typedResult == null) return "Something went wrong".ToErrorCallToolResponse();

        var server = await serverRepository.CreateServer(new SQL.Models.Server()
        {
            Name = typedResult.Name.Slugify(),
            Instructions = typedResult.Instructions,
            Title = typedResult.Title,
            Description = typedResult.Description,
            WebsiteUrl = typedResult.WebsiteUrl?.ToString(),
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
            server.WebsiteUrl,
            server.Hidden,
            SecurityGroups = server.Groups.Select(z => z.Id)
        })
        .ToJsonCallToolResponse($"mcp-editor://server/{serverName}");
    });

    [Description("Updates a MCP-server")]
    [McpServerTool(Title = "Update an MCP-server",
        OpenWorld = false)]
    public static async Task<CallToolResult> ModelContextEditor_UpdateServer(
      [Description("Name of the server")] string serverName,
      IServiceProvider serviceProvider,
      RequestContext<CallToolRequestParams> requestContext,
        [Description("New server title")]
        string? serverTitle = null,
        [Description("Optional description for the server")]
        string? serverDescription = null,
        [Description("Website url")]
        string? websiteUrl = null,
        [Description("Updated instructions for the server")]
        string? instructions = null,
        [Description("If the server should be hidden")]
        bool? hidden = null,
      CancellationToken cancellationToken = default)
    {
        var server = await serviceProvider.GetServer(serverName, cancellationToken);
        var (typed, notAccepted, result) = await requestContext.Server.TryElicit(new UpdateMcpServer()
        {
            Name = serverName,
            WebsiteUrl = string.IsNullOrEmpty(websiteUrl) ? null : new Uri(websiteUrl),
            Title = serverTitle ?? server.Title,
            Description = serverDescription ?? server.Description,
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
        server.Title = typed.Title;
        server.Description = typed.Description;
        server.WebsiteUrl = typed.WebsiteUrl?.ToString();

        var serverRepository = serviceProvider.GetRequiredService<ServerRepository>();
        var updated = await serverRepository.UpdateServer(server);

        return JsonSerializer.Serialize(new
        {
            server.Name,
            Owners = server.Owners.Select(z => z.Id),
            server.Secured,
            server.Description,
            server.WebsiteUrl,
            SecurityGroups = server.Groups.Select(z => z.Id),
            server.Hidden
        })
        .ToJsonCallToolResponse($"mcp-editor://server/{serverName}");
    }

    [Description("Deletes a MCP-server")]
    [McpServerTool(Title = "Delete an MCP-server",
        OpenWorld = false)]
    public static async Task<CallToolResult?> ModelContextEditor_DeleteServer(
        [Description("Name of the server")] string serverName,
        RequestContext<CallToolRequestParams> requestContext,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken = default) => await requestContext.WithExceptionCheck(async () =>
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
    });

    [Description("Adds an plugin to a MCP-server")]
    [McpServerTool(Title = "Add an plugin to a MCP-server",
        OpenWorld = false)]
    public static async Task<CallToolResult> ModelContextEditor_AddPlugin(
       [Description("Name of the server")] string serverName,
       [Description("Name of the plugin")] string pluginName,
       IServiceProvider serviceProvider,
       RequestContext<CallToolRequestParams> requestContext,
       CancellationToken cancellationToken = default)
    {
        var server = await serviceProvider.GetServer(serverName, cancellationToken);
        var (typed, notAccepted, result) = await requestContext.Server.TryElicit(new McpServerPlugin()
        {
            PluginName = pluginName
        }, cancellationToken);

        if (notAccepted != null) return notAccepted;
        if (typed == null) return "Something went wrong".ToErrorCallToolResponse();
        var repo = serviceProvider.GetRequiredService<IReadOnlyList<ServerConfig>>();
        HashSet<string> allPlugins = repo.GetAllPlugins();

        if (allPlugins.Any(a => a == typed.PluginName) != true)
        {
            return $"Plugin {typed.PluginName} not found".ToErrorCallToolResponse();
        }

        if (server.Tools.Any(a => a.Name == typed.PluginName) == true)
        {
            return $"Plugin {typed.PluginName} already exists on server {serverName}.".ToErrorCallToolResponse();
        }

        var serverRepository = serviceProvider.GetRequiredService<ServerRepository>();

        await serverRepository.AddServerTool(server.Id, typed.PluginName);

        return $"Plugin {typed.PluginName} added to MCP server {serverName}".ToTextCallToolResponse();
    }

    [Description("Get tools from a plugin")]
    [McpServerTool(Title = "Get plugin tools",
       OpenWorld = false)]
    public static async Task<CallToolResult?> ModelContextEditor_GetPluginTools(
      [Description("Name of the plugin")] string pluginName,
      IServiceProvider serviceProvider,
      RequestContext<CallToolRequestParams> requestContext,
      CancellationToken cancellationToken = default) => await requestContext.WithExceptionCheck(async () =>
    {
        var kernel = serviceProvider.GetRequiredService<Kernel>();

        return await Task.FromResult(
                kernel.GetToolsFromType(pluginName).ToJsonContentBlock("mcp-editor://tools").ToCallToolResult());
    });

    [Description("Get all tools from all plugins. Name and descriptions only.")]
    [McpServerTool(Title = "Get all tools",
      OpenWorld = false)]
    public static async Task<CallToolResult?> ModelContextEditor_GetAllTools(
     IServiceProvider serviceProvider,
     RequestContext<CallToolRequestParams> requestContext,
     CancellationToken cancellationToken = default) => await requestContext.WithExceptionCheck(async () =>
   {
       var repo = serviceProvider.GetRequiredService<IReadOnlyList<ServerConfig>>();
       var kernel = serviceProvider.GetRequiredService<Kernel>();

       return await Task.FromResult(repo.GetAllPlugins().SelectMany(v => kernel.GetToolsFromType(v)?.Select(a => new
       {
           Plugin = v,
           a.ProtocolTool.Name,
           a.ProtocolTool.Description
       }) ?? []).ToJsonContentBlock("mcp-editor://tools").ToCallToolResult());
   });

    [Description("Removes a plugin from a MCP-server")]
    [McpServerTool(Title = "Remove a plugin from an MCP-server",
        OpenWorld = false)]
    public static async Task<CallToolResult> ModelContextEditor_RemovePlugin(
       [Description("Name of the server")] string serverName,
       [Description("Name of the plugin")] string pluginName,
       IServiceProvider serviceProvider,
       RequestContext<CallToolRequestParams> requestContext,
       CancellationToken cancellationToken = default)
    {
        var server = await serviceProvider.GetServer(serverName, cancellationToken);

        if (server.Tools.Any(a => a.Name == pluginName) != true)
        {
            return $"Plugin {pluginName} is not a plugin on server {serverName}.".ToErrorCallToolResponse();
        }

        var (typed, notAccepted, result) = await requestContext.Server.TryElicit(new McpServerPlugin()
        {
            PluginName = pluginName
        }, cancellationToken);

        if (notAccepted != null) return notAccepted;
        if (typed == null) return "Something went wrong".ToErrorCallToolResponse();
        var serverRepository = serviceProvider.GetRequiredService<ServerRepository>();

        await serverRepository.DeleteServerPlugin(server.Id, typed.PluginName);

        return $"Plugin {typed.PluginName} deleted from MCP server {serverName}".ToTextCallToolResponse();
    }

}

