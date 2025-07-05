using MCPhappey.Common.Models;
using MCPhappey.Core.Services;
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
                 var kernel = ctx.RequestServices.GetRequiredService<Kernel>();
                 var completionService = ctx.RequestServices.GetRequiredService<CompletionService>();
                 var serverName = ctx.Request.Path.Value!.GetServerNameFromUrl();
                 var server = servers.First(a => a.Server.ServerInfo?.Name.Equals(serverName, StringComparison.OrdinalIgnoreCase) == true);
                 var headers = ctx.Request.Headers
                        .ToDictionary(h => h.Key, h => h.Value.ToString());

                 opts.ServerInfo = server.Server.ToServerInfo();
                 opts.ServerInstructions = server.Server.Instructions;
                 opts.Capabilities = new()
                 {
                     Resources = server.ToResourcesCapability(headers),
                     Prompts = server.ToPromptsCapability(headers),
                     Completions = server.ToCompletionsCapability(completionService, headers),
                     Tools = server.Server.ToToolsCapability(kernel, headers)
                 };

                 await Task.CompletedTask;
             };

        });
}
