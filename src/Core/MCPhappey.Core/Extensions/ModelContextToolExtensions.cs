using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using HtmlAgilityPack;
using MCPhappey.Common.Extensions;
using MCPhappey.Common.Models;
using Microsoft.SemanticKernel;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace MCPhappey.Core.Extensions;

public static partial class ModelContextToolExtensions
{
    public static async Task<CallToolResult?> WithExceptionCheck(this RequestContext<CallToolRequestParams> requestContext, Func<Task<CallToolResult?>> func)
    {
        try
        {
            return await func();
        }
        catch (Exception e)
        {
            return e.Message.ToErrorCallToolResponse();
        }
    }

    public static ToolsCapability? ToToolsCapability(this Server server, Kernel kernel,
        Dictionary<string, string>? headers = null)
    {
        if (server.Plugins?.Any() != true) return null;

        List<McpServerTool>? tools = [];

        foreach (var pluginTypeName in server.Plugins ?? [])
        {
            tools.AddRange(kernel.GetToolsFromType(pluginTypeName) ?? []);
        }

        return tools.BuildCapability(server, headers);
    }

    public static IEnumerable<McpServerTool>? GetToolsFromType(this Kernel kernel, string pluginTypeName)
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


    private static ToolsCapability? BuildCapability(this IEnumerable<McpServerTool>? tools, Server server,
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
                    var tool = tools.FirstOrDefault(a => a.ProtocolTool.Name == request.Params?.Name);

                    if (tool == null)
                    {
                        return JsonSerializer.Serialize($"Tool {tool?.ProtocolTool.Name} not found").ToErrorCallToolResponse();
                    }

                    request.Services!.WithHeaders(headers);

                    return await tool.InvokeAsync(request, cancellationToken);
                }
        };
    }
}