
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using MCPhappey.Core.Models.Protocol;
using MCPhappey.Core.Services;
using MCPhappey.Core.Constants;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using ModelContextProtocol.Protocol.Types;
using ModelContextProtocol.Server;

namespace MCPhappey.Core.Extensions;

public static partial class ModelContextExtensions
{
    public static GradioPlugin ToGradio(this ServerConfig server, HttpContext httpContext)
     => new()
     {
         Id = server.Server.ServerInfo.Name,
         Title = server.Server.ServerInfo.Name,
         Transport = new()
         {
             Url = server.Server.GetUrl(httpContext)
         }
     };


    public static MCPServerList ToMcpServerList(this IEnumerable<ServerConfig> servers, HttpContext httpContext)
     => new()
     {
         McpServers = servers
                    .ToDictionary(a => a.Server.ServerInfo.Name, a => new MCPServer()
                    {
                        Type = "sse",
                        Headers = a.Auth != null ? new() {
                            {"Authorization", "Bearer your-token"}
                           } : null,
                        Url = a.Server.GetUrl(httpContext)
                    })
     };

    public static Implementation ToServerInfo(this Server server)
        => new() { Name = server.ServerInfo.Name, Version = server.ServerInfo.Version };

    public static string GetServerRelativeUrl(this Server server)
        => $"/{ServerMetadata.McpServerRoot}/{server.ServerInfo.Name.ToLowerInvariant()}";

    public static ResourcesCapability? ToResourcesCapability(this Server server, HttpContext httpContext)
        => server.Capabilities.Resources != null ?
            new ResourcesCapability()
            {
                ListResourcesHandler = async (request, cancellationToken)
                    =>
                {
                    var service = request.Services!.GetRequiredService<ResourceService>();

                    return await service.GetServerResources(server, httpContext, cancellationToken);
                },
                ListResourceTemplatesHandler = async (request, cancellationToken)
                 =>
                {
                    var service = request.Services!.GetRequiredService<ResourceService>();

                    return await service.GetServerResourceTemplates(server);
                },
                ReadResourceHandler = async (request, cancellationToken) =>
                {
                    var scraper = request.Services!.GetRequiredService<ResourceService>();

                    return await scraper.GetServerResource(server, request.Params?.Uri!,
                        httpContext, cancellationToken);
                },
            }
            : null;

    public static PromptsCapability? ToPromptsCapability(this Server server, HttpContext httpContext)
        => server.Capabilities.Prompts != null ?
            new PromptsCapability()
            {
                ListPromptsHandler = async (request, cancellationToken)
                    =>
                {
                    var service = request.Services!.GetRequiredService<PromptService>();

                    return await service.GetServerPrompts(server, cancellationToken);
                },
                GetPromptHandler = async (request, cancellationToken)
                    =>
                {
                    var service = request.Services!.GetRequiredService<PromptService>();

                    return await service.GetServerPrompt(server, request.Params?.Name!,
                        request.Params?.Arguments ?? new Dictionary<string, JsonElement>(),
                        httpContext,
                        cancellationToken);
                }
            }
            : null;

    public static ToolsCapability? ToToolsCapability(this Server server, Kernel kernel)
    {
        if (server.Metadata == null ||
            !server.Metadata.TryGetValue(ServerMetadata.Plugins, out var pluginValueObj))
            return null;

        var pluginTypeNames = pluginValueObj?.ToString().Split(";") ?? [];
        if (pluginTypeNames.Length == 0) return null;

        List<McpServerTool>? tools = [];

        foreach (var pluginTypeName in pluginTypeNames)
        {
            tools.AddRange(kernel.GetToolsFromType(pluginTypeName) ?? []);
        }

        return tools.BuildCapability();
    }

    private static IEnumerable<McpServerTool>? GetToolsFromType(this Kernel kernel, string pluginTypeName)
    {
        var pluginType = Type.GetType(pluginTypeName);

        if (pluginType == null)
        {
            return null;
        }

        IEnumerable<McpServerTool>? pluginTools = pluginType.IsAbstract && pluginType.IsSealed
            ? pluginType?
                .GetMethods(BindingFlags.Public |
                            BindingFlags.Static |
                            BindingFlags.Instance |
                            BindingFlags.DeclaredOnly)
                .Select(a => McpServerTool.Create(a))
            : kernel.CreatePluginFromObject(Activator.CreateInstance(pluginType)!)
                    .AsAIFunctions()
                  .Select(a => McpServerTool.Create(a));

        return pluginTools;
    }

    private static ToolsCapability? BuildCapability(this IEnumerable<McpServerTool>? tools)
    {
        if (tools == null || !tools.Any())
            return null;

        var collection = new McpServerPrimitiveCollection<McpServerTool>();

        foreach (var tool in tools)
            collection.Add(tool);

        return new ToolsCapability { ToolCollection = collection };
    }

    public static string GetUrl(this Server server, HttpContext httpContext) =>
        $"{httpContext.Request.Scheme}://{httpContext.Request.Host}{server.GetServerRelativeUrl()}/sse";

    public static string FormatWith(this string template, IReadOnlyDictionary<string, JsonElement> values)
    {
        ArgumentNullException.ThrowIfNull(template);
        ArgumentNullException.ThrowIfNull(values);

        return PromptArgumentRegex().Replace(template, match =>
        {
            var key = match.Groups[1].Value;
            return values.TryGetValue(key, out var value) ? value.ToString() ?? string.Empty : match.Value;
        });
    }

    [GeneratedRegex("{(.*?)}")]
    private static partial Regex PromptArgumentRegex();

}