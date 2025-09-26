using MCPhappey.Common.Models;
using MCPhappey.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using ModelContextProtocol.Protocol;

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
                 var headers = ctx.Request.Headers.Where(a => server.Server.Headers?.ContainsKey(a.Key) == true ||
                    (server.Server.OBO?.Count > 0 && a.Key == "Authorization"))
                        .ToDictionary(h => h.Key, h => h.Value.ToString());

                 if (completionService.CanComplete(server, cancellationToken))
                 {
                     opts.Handlers.CompleteHandler = async (request, cancellationToken)
                            => await request.ToCompleteResult(server, completionService, headers,
                                cancellationToken: cancellationToken)
                                ?? new CompleteResult();
                 }

                 if (server.Server.Capabilities.Tools != null)
                 {
                     opts.Handlers.ListToolsHandler = async (request, cancellationToken)
                           => await server.Server.ToToolsList(kernel, headers)
                            ?? new ListToolsResult();

                     opts.Handlers.CallToolHandler = async (request, cancellationToken)
                                => await request.ToCallToolResult(server.Server, kernel, headers)
                                    ?? new CallToolResult();
                 }

                 if (server.Server.Capabilities.Prompts != null)
                 {
                     opts.Handlers.ListPromptsHandler = async (request, cancellationToken)
                            => await server.ToListPromptsResult(request, cancellationToken)
                                ?? new ListPromptsResult();

                     opts.Handlers.GetPromptHandler = async (request, cancellationToken)
                             => await request.ToGetPromptResult(headers, cancellationToken)!
                                 ?? new GetPromptResult();
                 }

                 if (server.Server.Capabilities.Resources != null)
                 {
                     opts.Handlers.ListResourcesHandler = async (request, cancellationToken) =>
                         await server.ToListResourcesResult(request, headers, cancellationToken)
                             ?? new ListResourcesResult();

                     opts.Handlers.ListResourceTemplatesHandler = async (request, cancellationToken) =>
                         await server.ToListResourceTemplatesResult(request, headers, cancellationToken)
                             ?? new ListResourceTemplatesResult();

                     opts.Handlers.ReadResourceHandler = async (request, cancellationToken) =>
                         await request.ToReadResourceResult(headers, cancellationToken)
                             ?? new ReadResourceResult();
                 }

                 opts.ServerInfo = server.Server.ToServerInfo();
                 opts.ServerInstructions = server.Server.Instructions;
                 opts.Capabilities = new()
                 {
                     //   Resources = server.ToResourcesCapability(),
                     //   Prompts = server.ToPromptsCapability(),
                     // Completions = server.ToCompletionsCapability(completionService, headers),
                     // Tools = await server.Server.ToToolsCapability(kernel, headers)
                 };

                 await Task.CompletedTask;
             };
        });
}
