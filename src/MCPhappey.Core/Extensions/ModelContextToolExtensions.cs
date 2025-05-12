
using System.Reflection;
using MCPhappey.Common.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using ModelContextProtocol.Protocol.Types;
using ModelContextProtocol.Server;
using MCPhappey.Auth.Models;
using MCPhappey.Common.Constants;

namespace MCPhappey.Core.Extensions;

public static partial class ModelContextToolExtensions
{
    public static ToolsCapability? ToToolsCapability(this Server server, Kernel kernel,
    string? authToken = null)
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

        return tools.BuildCapability(authToken);
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

    private static ToolsCapability? BuildCapability(this IEnumerable<McpServerTool>? tools,
        string? authToken = null)
    {
        if (tools == null || !tools.Any())
            return null;

        var collection = new McpServerPrimitiveCollection<McpServerTool>();

        foreach (var tool in tools)
            collection.Add(tool);

        return new ToolsCapability
        {
            ListToolsHandler = async (request, cancellationToken)
                    =>
                {
                    return await Task.FromResult(new ListToolsResult()
                    {
                        Tools = [.. tools.Select(a => a.ProtocolTool)],
                    });
                },
            CallToolHandler = async (request, cancellationToken)
                =>
                {
                    var tool = tools.First(a => a.ProtocolTool.Name == request.Params?.Name);
                    var tokenProvider = request.Services?.GetService<TokenProvider>();

                    if (tokenProvider != null)
                    {
                        tokenProvider!.Token = authToken;
                    }

                    return await tool.InvokeAsync(request, cancellationToken);
                }
        };
    }
}