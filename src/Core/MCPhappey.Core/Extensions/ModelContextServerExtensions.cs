using MCPhappey.Common.Extensions;
using MCPhappey.Common.Models;
using Microsoft.AspNetCore.Http;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace MCPhappey.Core.Extensions;

public static partial class ModelContextServerExtensions
{
    public static GradioPlugin ToGradio(this ServerConfig server, HttpContext httpContext)
     => new()
     {
         Id = server.Server.ServerInfo.Name,
         Title = server.Server.ServerInfo.Title ?? server.Server.ServerInfo.Name,
         Transport = new()
         {
             Url = server.Server.GetUrl(httpContext)
         }
     };

    public static ServerConfig? GetServerConfig(this IEnumerable<ServerConfig> servers, McpServer mcpServer)
       => servers.GetServerConfig(mcpServer.ServerOptions.ServerInfo?.Name!);

    public static ServerConfig? GetServerConfig(this IEnumerable<ServerConfig> servers, string name)
       => servers.FirstOrDefault(s =>
                  name?.Equals(s.Server.ServerInfo.Name,
                    StringComparison.OrdinalIgnoreCase) == true);

    public static IEnumerable<ServerConfig> WithoutHiddenServers(this IEnumerable<ServerConfig> servers)
        => servers.Where(a => a.Server.Hidden != true);

    public static MCPServerRegistry ToMcpServerRegistry(this IEnumerable<ServerConfig> servers, string baseUrl, bool sse = false)
     => new()
     {
         Servers = servers
            .WithoutHiddenServers()
            .OrderBy(a => a.Server.ServerInfo.Name)
            .Select(a => a.ToMCPServerRegistryItem(baseUrl))
     };

    public static MCPServerList ToMcpServerList(this IEnumerable<ServerConfig> servers, string baseUrl, bool sse = false)
        => new()
        {
            Servers = servers
                .WithoutHiddenServers()
                .OrderBy(a => a.Server.ServerInfo.Name)
                .ToDictionary(a => a.Server.ServerInfo.Name, a => sse ? a.ToSseMcpServer(baseUrl)
                        : a.ToMcpServer(baseUrl))
        };


    public static MCPServerSettingsList ToMcpServerSettingsList(this IEnumerable<ServerConfig> servers, string baseUrl)
     => new()
     {
         McpServers = servers
                .WithoutHiddenServers()
                .OrderBy(a => a.Server.ServerInfo.Name)
                .ToDictionary(a => a.Server.ServerInfo.Name, a => a.ToMcpServerSettings(baseUrl))
     };

    public static string WithoutScheme(this string url)
          => new Uri(url).Authority.TrimEnd('/');

    public static MCPServerRegistryItem ToMCPServerRegistryItem(this ServerConfig server, string baseUrl)
               => new()
               {
                   Server = server.ToRegistryServer(baseUrl)
               };

    public static RegistryServer ToRegistryServer(this ServerConfig server, string baseUrl)
            => new()
            {
                Description = server.Server.ServerInfo.Description,
                Version = server.Server.ServerInfo.Version,
                WebsiteUrl = server.Server.ServerInfo.WebsiteUrl,
                Title = server.Server.ServerInfo.Title,
                Icons = server.Server.ServerInfo.Icons?.Select(a => new ServerIcon()
                {
                    Source = a.Source,
                    MimeType = a.MimeType,
                    Sizes = a.Sizes,
                    Theme = a.Theme
                }),
                Name = $"{baseUrl.WithoutScheme()}/{server.Server.ServerInfo.Name}",
                Repository = new()
                {
                    Source = "github",
                    Url = "https://github.com/achappey/MCPhappey",
                    Subfolder = server.SourceType == ServerSourceType.Static ?
                        $"/tree/master/src/Servers/MCPhappey.Servers.JSON/Servers/{server.Server.ServerInfo.Name}" :
                        "/tree/master/src/Servers/MCPhappey.Servers.SQL"
                },
                Remotes = [new ServerRemote() {
                    Url =  $"{baseUrl}/{server.Server.ServerInfo.Name.ToLowerInvariant()}",
                }]
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
        => new()
        {
            Name = server.ServerInfo.Name,
            Title = server.ServerInfo.Title,
            Version = server.ServerInfo.Version,
            Icons = server.ServerInfo.Icons?.ToList()
        };

    public static string GetServerRelativeUrl(this Server server)
        => $"/{server.ServerInfo.Name.ToLowerInvariant()}";


    public static string GetUrl(this Server server, HttpContext httpContext) =>
        $"{httpContext.Request.Scheme}://{httpContext.Request.Host}{server.GetServerRelativeUrl()}";


    public static async Task<ResourceLinkBlock?> Upload(this McpServer mcpServer,
                IServiceProvider serviceProvider,
                string filename,
                BinaryData binaryData,
                CancellationToken cancellationToken = default)
    {
        using var client = await serviceProvider.GetOboGraphClient(mcpServer);

        var sizeInKb = binaryData.Length / 1024.0;
        var markdown = $"Upload {filename} ({sizeInKb:F1} KB)";
        await mcpServer.SendMessageNotificationAsync(markdown, LoggingLevel.Info);

        return await client.Upload(filename, binaryData, cancellationToken);
    }



}