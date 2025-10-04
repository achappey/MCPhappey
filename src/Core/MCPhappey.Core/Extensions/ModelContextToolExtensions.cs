using System.Reflection;
using System.Security.Claims;
using System.Text.Json;
using MCPhappey.Common.Extensions;
using MCPhappey.Common.Models;
using MCPhappey.Telemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using ModelContextProtocol.Client;
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

    public static async Task<ListToolsResult?> ToToolsList(this Server server, Kernel kernel,
            Dictionary<string, string>? headers = null)
    {
        if (server.Plugins?.Any() != true && server.McpExtension == null) return null;

        List<McpServerTool>? tools = [];

        foreach (var pluginTypeName in server.Plugins ?? [])
        {
            tools.AddRange(kernel.GetToolsFromType(pluginTypeName) ?? []);
        }

        return await tools.GetListToolsResult(server, headers);
    }

    public static async Task<CallToolResult?> ToCallToolResult(this RequestContext<CallToolRequestParams> request,
        Server server, Kernel kernel,
        Dictionary<string, string>? headers = null,
        CancellationToken cancellationToken = default)
    {
        var telemtry = request.Services!.GetService<IMcpTelemetryService>();
        var userId = request.User?.FindFirstValue("http://schemas.microsoft.com/identity/claims/objectidentifier");
        var startTime = DateTime.UtcNow;

        if (server.Plugins?.Any() != true && server.McpExtension == null) return null;

        List<McpServerTool>? tools = [];

        foreach (var pluginTypeName in server.Plugins ?? [])
        {
            tools.AddRange(kernel.GetToolsFromType(pluginTypeName) ?? []);
        }

        var result = await request.GetCallToolResult(tools, server, headers, cancellationToken: cancellationToken);

        var endTime = DateTime.UtcNow;

        if (telemtry != null)
        {
            await telemtry.TrackToolRequestAsync(request.Server.ServerOptions.ServerInfo?.Name!,
                request.Server.SessionId!,
                request.Server.ClientInfo?.Name!,
                request.Server.ClientInfo?.Version!,
                request.Params?.Name!,
                result.GetJsonSizeInBytes(), startTime, endTime, userId, request.User?.Identity?.Name, cancellationToken);
        }

        return result;
    }

    /*   public static async Task<ToolsCapability?> ToToolsCapability(this Server server, Kernel kernel,
           Dictionary<string, string>? headers = null)
       {
           if (server.Plugins?.Any() != true && server.McpExtension == null) return null;

           List<McpServerTool>? tools = [];

           foreach (var pluginTypeName in server.Plugins ?? [])
           {
               tools.AddRange(kernel.GetToolsFromType(pluginTypeName) ?? []);
           }

           return await tools.BuildCapability(server, headers);
       }*/

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

    private static async Task<ListToolsResult?> GetListToolsResult(this IEnumerable<McpServerTool>? tools, Server server,
        Dictionary<string, string>? headers = null)
    {
        if (server.McpExtension != null) return await server.ExtendListToolsCapabilities();

        if (tools == null || !tools.Any())
            return null;

        var collection = new McpServerPrimitiveCollection<McpServerTool>();

        foreach (var tool in tools)
            collection.Add(tool);

        return await Task.FromResult(new ListToolsResult()
        {
            Tools = [.. tools.Select(a => a.ProtocolTool)],
        });

    }
    /*
        private static async Task<ToolsCapability?> BuildCapability(this IEnumerable<McpServerTool>? tools,
            Server server,
            Dictionary<string, string>? headers = null)
        {
            if (server.McpExtension != null) return await server.ExtendCapabilities();

            if (tools == null || !tools.Any())
                return null;

            //   var collection = new McpServerPrimitiveCollection<McpServerTool>();

            //    foreach (var tool in tools)
            //     collection.Add(tool);

            return new ToolsCapability
            {
                /* ListToolsHandler = async (request, cancellationToken)
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
                     }*/
    //  };
    //}

    private static async Task<CallToolResult?> GetCallToolResult(
            this RequestContext<CallToolRequestParams> request,
            IEnumerable<McpServerTool>? tools, Server server,
            Dictionary<string, string>? headers = null,
            CancellationToken cancellationToken = default)
    {
        if (server.McpExtension != null) return await request.ExtendListToolsCapabilities(server);

        if (tools == null || !tools.Any())
            return null;

        var tool = tools.FirstOrDefault(a => a.ProtocolTool.Name == request.Params?.Name);

        if (tool == null)
        {
            return JsonSerializer.Serialize($"Tool {tool?.ProtocolTool.Name} not found").ToErrorCallToolResponse();
        }

        request.Services!.WithHeaders(headers);

        return await tool.InvokeAsync(request, cancellationToken);

    }
    /*
        private static async Task<ToolsCapability?> ExtendCapabilities(this Server server,
            Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default)
        {
            var client = await McpClient.CreateAsync(
                      new HttpClientTransport(new HttpClientTransportOptions
                      {
                          Endpoint = new Uri(server.McpExtension?.Url!),

                      }),
                      clientOptions: new McpClientOptions()
                      {
                      },
                      cancellationToken: cancellationToken);

            if (client.ServerCapabilities.Tools == null) return null;

            return new ToolsCapability
            {
                /*     ListToolsHandler = async (request, cancellationToken)
                                =>
                            {
                                await using var client = await McpClientFactory.CreateAsync(
                                 new HttpClientTransport(new HttpClientTransportOptions
                                 {
                                     Endpoint = new Uri(server.McpExtension?.Url!),
                                 }),
                                 clientOptions: new McpClientOptions()
                                 {
                                     ClientInfo = request.Server.ClientInfo,
                                     Capabilities = new ClientCapabilities()
                                     {
                                         Sampling = request.Server.ClientCapabilities?.Sampling?.SamplingHandler != null ? new SamplingCapability()
                                         {
                                             SamplingHandler = request.Server.ClientCapabilities?.Sampling?.SamplingHandler
                                         } : null
                                     }
                                 },
                                 cancellationToken: cancellationToken);
                                var tools = await client.ListToolsAsync(cancellationToken: cancellationToken);

                                return await Task.FromResult(new ListToolsResult()
                                {
                                    Tools = [.. tools.Select(a => a.ProtocolTool)],
                                });
                            },
                     CallToolHandler = async (request, cancellationToken)
                         =>
                         {
                             await using var client = await McpClient.CreateAsync(
                                new HttpClientTransport(new HttpClientTransportOptions
                                {
                                    Endpoint = new Uri(server.McpExtension?.Url!),
                                }),
                                clientOptions: new McpClientOptions()
                                {
                                    ClientInfo = request.Server.ClientInfo,
                                    Capabilities = new ClientCapabilities()
                                    {
                                        Sampling = request.Server.ClientCapabilities?.Sampling?.SamplingHandler != null ? new SamplingCapability()
                                        {
                                            SamplingHandler = request.Server.ClientCapabilities?.Sampling?.SamplingHandler
                                        } : null
                                    }
                                },
                                cancellationToken: cancellationToken);

                             var tools = await client.ListToolsAsync(cancellationToken: cancellationToken);
                             var tool = tools.FirstOrDefault(a => a.ProtocolTool.Name == request.Params?.Name);

                             if (tool == null)
                             {
                                 return JsonSerializer.Serialize($"Tool {tool?.ProtocolTool.Name} not found").ToErrorCallToolResponse();
                             }

                             request.Services!.WithHeaders(headers);

                             var args = request.Params?.Arguments?
                                 .ToDictionary(
                                     kvp => kvp.Key,
                                     kvp => kvp.Value.ValueKind == JsonValueKind.Undefined
                                         ? null
                                         : kvp.Value.Deserialize<object?>()
                                 );

                             return await client.CallToolAsync(tool?.ProtocolTool.Name!, args, cancellationToken: cancellationToken);
                         }*/
    //  };
    //}

    private static async Task<ListToolsResult?> ExtendListToolsCapabilities(this Server server,
        Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default)
    {
        var client = await McpClient.CreateAsync(
                  new HttpClientTransport(new HttpClientTransportOptions
                  {
                      Endpoint = new Uri(server.McpExtension?.Url!),

                  }),
                  clientOptions: new McpClientOptions()
                  {
                      Capabilities = new ClientCapabilities()
                      {
                      }
                  },
                  cancellationToken: cancellationToken);

        if (client.ServerCapabilities.Tools == null) return null;

        var tools = await client.ListToolsAsync(cancellationToken: cancellationToken);

        return await Task.FromResult(new ListToolsResult()
        {
            Tools = [.. tools.Select(a => a.ProtocolTool)],
        });
    }

    private static async Task<CallToolResult?> ExtendListToolsCapabilities(this ModelContextProtocol.Server.RequestContext<CallToolRequestParams> request,
        Server server,
       Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default)
    {
        var client = await McpClient.CreateAsync(
                  new HttpClientTransport(new HttpClientTransportOptions
                  {
                      Endpoint = new Uri(server.McpExtension?.Url!),

                  }),
                  clientOptions: new McpClientOptions()
                  {
                      Handlers = new McpClientHandlers()
                      {
                          SamplingHandler = request.Server.ClientCapabilities?.Sampling != null
                            ? async (req, progress, cancellationToken) =>
                          {
                              return await request.Server.SampleAsync(req!, cancellationToken);
                          }
                          : null
                      },
                      Capabilities = new ClientCapabilities()
                      {
                          Sampling = request.Server.ClientCapabilities?.Sampling
                      }
                  },
                  cancellationToken: cancellationToken);


        if (client.ServerCapabilities.Tools == null) return null;

        var tools = await client.ListToolsAsync(cancellationToken: cancellationToken);
        var tool = tools.FirstOrDefault(a => a.ProtocolTool.Name == request.Params?.Name);

        if (tool == null)
        {
            return JsonSerializer.Serialize($"Tool {tool?.ProtocolTool.Name} not found").ToErrorCallToolResponse();
        }

        request.Services!.WithHeaders(headers);

        var args = request.Params?.Arguments?
            .ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.ValueKind == JsonValueKind.Undefined
                    ? null
                    : kvp.Value.Deserialize<object?>()
            );

        return await client.CallToolAsync(tool?.ProtocolTool.Name!, args, cancellationToken: cancellationToken);
    }
}