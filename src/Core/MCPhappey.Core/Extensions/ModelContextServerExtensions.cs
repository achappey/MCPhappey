using MCPhappey.Common.Extensions;
using MCPhappey.Common.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Graph.Beta;
using Microsoft.KernelMemory.DataFormats;
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

    public static IEnumerable<ServerConfig> WithoutHiddenServers(this IEnumerable<ServerConfig> servers)
        => servers.Where(a => a.Server.Hidden != true);


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


    public static async Task<ResourceLinkBlock?> Upload(this IMcpServer mcpServer,
                IServiceProvider serviceProvider,
                string filename,
                BinaryData binaryData,
                CancellationToken cancellationToken = default)
    {
        var client = await serviceProvider.GetOboGraphClient(mcpServer);

        var sizeInKb = binaryData.Length / 1024.0;
        var markdown = $"Upload {filename} ({sizeInKb:F1} KB)";
        await mcpServer.SendMessageNotificationAsync(markdown, LoggingLevel.Info);

        return await client.Upload(filename, binaryData, cancellationToken);
    }


    public static async Task<FileItem> DecodeAsync(this ServiceProvider serviceProvider, string uri, BinaryData binaryData, string contentType,
           CancellationToken cancellationToken = default)
    {
        var contentDecoders = serviceProvider.GetService<IEnumerable<IContentDecoder>>();

        if (contentType.StartsWith("image/"))
        {
            return new FileItem
            {
                Contents = binaryData,
                MimeType = contentType,
                Uri = uri
            };
        }

        string? myAssemblyName = typeof(ModelContextServerExtensions).Namespace?.Split(".").FirstOrDefault();

        var bestDecoder = contentDecoders?
            .Where(a => a.SupportsMimeType(contentType))
            .OrderBy(d => myAssemblyName != null
                && d.GetType().Namespace?.Contains(myAssemblyName) == true ? 0 : 1)
            .FirstOrDefault();

        FileContent? fileContent = null;
        if (bestDecoder != null)
        {
            fileContent = await bestDecoder.DecodeAsync(binaryData, cancellationToken);
        }

        // Fallback: original content if nothing could decode
        return fileContent != null
            ? fileContent.GetFileItemFromFileContent(uri)
            : new FileItem
            {
                Contents = binaryData,
                MimeType = contentType,
                Uri = uri
            };
    }
}