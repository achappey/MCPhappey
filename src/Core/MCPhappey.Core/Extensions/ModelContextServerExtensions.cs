using MCPhappey.Common.Models;
using Microsoft.AspNetCore.Http;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace MCPhappey.Core.Extensions;

public static partial class ModelContextServerExtensions
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

    public static ServerConfig? GetServerConfig(this IEnumerable<ServerConfig> servers, IMcpServer mcpServer)
       => servers.GetServerConfig(mcpServer.ServerOptions.ServerInfo?.Name!);

    public static ServerConfig? GetServerConfig(this IEnumerable<ServerConfig> servers, string name)
       => servers.FirstOrDefault(s =>
                  name?.Equals(s.Server.ServerInfo.Name,
                    StringComparison.OrdinalIgnoreCase) == true);
    public static MCPServerList ToMcpServerList(this IEnumerable<ServerConfig> servers, string baseUrl, bool sse = false)
     => new()
     {
         Servers = servers
                .OrderBy(a => a.Server.ServerInfo.Name)
                    .ToDictionary(a => a.Server.ServerInfo.Name, a => sse ? a.ToSseMcpServer(baseUrl)
                        : a.ToMcpServer(baseUrl))
     };

    public static MCPServerSettingsList ToMcpServerSettingsList(this IEnumerable<ServerConfig> servers, string baseUrl)
     => new()
     {
         McpServers = servers
                    .OrderBy(a => a.Server.ServerInfo.Name)
                    .ToDictionary(a => a.Server.ServerInfo.Name, a => a.ToMcpServerSettings(baseUrl))
     };

    public static MCPServer ToMcpServer(this ServerConfig server, string baseUrl)
        => new()
        {
            Type = "http",
            Url = $"{baseUrl}/{server.Server.ServerInfo.Name.ToLowerInvariant()}",
            Headers = server.Server.Headers
        };

    public static MCPServer ToSseMcpServer(this ServerConfig server, string baseUrl)
        => new()
        {
            Type = "sse",
            Url = $"{baseUrl}/{server.Server.ServerInfo.Name.ToLowerInvariant()}/sse",
            Headers = server.Server.Headers
        };

    public static MCPServerSettings ToMcpServerSettings(this ServerConfig server, string baseUrl)
            => new()
            {
                TransportType = "http",
                Url = $"{baseUrl}/{server.Server.ServerInfo.Name.ToLowerInvariant()}"
            };

    public static Implementation ToServerInfo(this Server server)
        => new() { Name = server.ServerInfo.Name, Version = server.ServerInfo.Version };

    public static string GetServerRelativeUrl(this Server server)
        => $"/{server.ServerInfo.Name.ToLowerInvariant()}";


    public static string GetUrl(this Server server, HttpContext httpContext) =>
        $"{httpContext.Request.Scheme}://{httpContext.Request.Host}{server.GetServerRelativeUrl()}";

    public static async Task<int?> SendProgressNotificationAsync(
        this IMcpServer mcpServer,
        RequestContext<CallToolRequestParams> requestContext,
        int? progressCounter,
        string? message,
        CancellationToken cancellationToken = default)
        {
            var progressToken = requestContext.Params?.Meta?.ProgressToken;
            if (progressToken is not null && progressCounter is not null)
            {
                await mcpServer.SendNotificationAsync(
                    "notifications/progress",
                    new ProgressNotification
                    {
                        ProgressToken = progressToken.Value,
                        Progress = new ProgressNotificationValue
                        {
                            Progress = progressCounter.Value,
                            Message = message
                        }
                    },
                    cancellationToken: cancellationToken
                );

                return progressCounter++;
            }

            return progressCounter;
        }
}