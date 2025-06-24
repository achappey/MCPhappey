
using System.Reflection;
using MCPhappey.Common.Extensions;
using MCPhappey.Common.Models;
using Microsoft.SemanticKernel;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace MCPhappey.Core.Extensions;

public static partial class ModelContextToolExtensions
{
    public static ToolsCapability? ToToolsCapability(this Server server, Kernel kernel,
        Dictionary<string, string>? headers = null)
    {
        if (server.Plugins?.Any() != true) return null;

        List<McpServerTool>? tools = [];

        foreach (var pluginTypeName in server.Plugins ?? [])
        {
            tools.AddRange(kernel.GetToolsFromType(pluginTypeName) ?? []);
        }

        return tools.BuildCapability(headers);
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
        Dictionary<string, string>? headers = null)
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
                    request.Services!.WithHeaders(headers);

                    try
                    {
                        return await tool.InvokeAsync(request, cancellationToken);
                    }
                    catch (Exception e)
                    {
                        await request.Server.SendMessageNotificationAsync(e.ToString(), LoggingLevel.Error);

                        return e.Message.ToErrorCallToolResponse();
                    }
                }
        };
    }

}