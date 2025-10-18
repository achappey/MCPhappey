using System.ComponentModel;
using DocumentFormat.OpenXml.Wordprocessing;
using MCPhappey.Common.Extensions;
using MCPhappey.Common.Models;
using MCPhappey.Core.Extensions;
using MCPhappey.Servers.SQL.Extensions;
using MCPhappey.Servers.SQL.Repositories;
using MCPhappey.Servers.SQL.Tools.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace MCPhappey.Servers.SQL.Tools;

public static partial class ModelContextEditor
{
    [Description("Add an plugin to a MCP-server")]
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

        if (server.Plugins.Any(a => a.PluginName == typed.PluginName) == true)
        {
            return $"Plugin {typed.PluginName} already exists on server {serverName}.".ToErrorCallToolResponse();
        }

        var serverRepository = serviceProvider.GetRequiredService<ServerRepository>();

        await serverRepository.AddServerTool(server.Id, typed.PluginName);

        return $"Plugin {typed.PluginName} added to MCP server {serverName}".ToTextCallToolResponse();
    }

    [Description("Add tool output template to MCP-server to show as app widget after tool call.")]
    [McpServerTool(Title = "Add tool output template to MCP-server",
       OpenWorld = false)]
    public static async Task<CallToolResult> ModelContextEditor_AddToolOutputTemplate(
      [Description("Name of the server")] string serverName,
      [Description("Name of the tool")] string toolName,
      [Description("Uri of the outputTemplate")] string outputTemplate,
      IServiceProvider serviceProvider,
      RequestContext<CallToolRequestParams> requestContext,
      CancellationToken cancellationToken = default)
    {
        var server = await serviceProvider.GetServer(serverName, cancellationToken);
        var (typed, notAccepted, result) = await requestContext.Server.TryElicit(new McpServerToolTemplate()
        {
            ToolName = toolName,
            OutputTemplate = outputTemplate
        }, cancellationToken);

        if (notAccepted != null) return notAccepted;
        if (typed == null) return "Something went wrong".ToErrorCallToolResponse();
        var serverRepository = serviceProvider.GetRequiredService<ServerRepository>();

        await serverRepository.AddToolMetadata(server.Id, typed.ToolName, typed.OutputTemplate);

        return $"Tool output template {typed.OutputTemplate} for tool {typed.ToolName} added to MCP server {serverName}"
            .ToTextCallToolResponse();
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

        if (server.Plugins.Any(a => a.PluginName == pluginName) != true)
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

