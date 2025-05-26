using MCPhappey.Auth.Extensions;
using MCPhappey.Common.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;

namespace MCPhappey.Core.Extensions;

public static class ModelContextProtocolExtensions
{
    public static IMcpServerBuilder WithConfigureSessionOptions(this IMcpServerBuilder mcpBuilder,
        IEnumerable<ServerConfig> servers) => mcpBuilder.WithHttpTransport(http =>
        {

            http.ConfigureSessionOptions = async (ctx, opts, cancellationToken) =>
             {
                 await Task.Run(() =>
                 {
                     var kernel = ctx.RequestServices.GetRequiredService<Kernel>();
                     var serverName = ctx.Request.Path.Value!.GetServerNameFromUrl();
                     var server = servers.First(a => a.Server.ServerInfo?.Name.Equals(serverName, StringComparison.OrdinalIgnoreCase) == true);
                     var authToken = ctx.GetBearerToken();
                     var headers = ctx.Request.Headers
                            .ToDictionary(h => h.Key, h => h.Value.ToString());
                     opts.ServerInfo = server.Server.ToServerInfo();
                     opts.Capabilities = new()
                     {
                         Resources = server.ToResourcesCapability(headers),
                         Prompts = server.ToPromptsCapability(headers),
                         Tools = server.Server.ToToolsCapability(kernel, headers)
                     };
                 }, cancellationToken);
             };

        });
}
