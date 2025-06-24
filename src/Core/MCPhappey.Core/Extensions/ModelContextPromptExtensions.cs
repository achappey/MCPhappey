using System.Text.Json;
using MCPhappey.Common.Models;
using MCPhappey.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Protocol;

namespace MCPhappey.Core.Extensions;

public static partial class ModelContextPromptExtensions
{
    public static PromptsCapability? ToPromptsCapability(this ServerConfig serverConfig,
        Dictionary<string, string>? headers = null)
        => serverConfig.Server.Capabilities.Prompts != null ?
            new PromptsCapability()
            {
                ListPromptsHandler = async (request, cancellationToken)
                    =>
                {
                    var service = request.Services!.GetRequiredService<PromptService>();

                    return await service.GetServerPrompts(serverConfig, cancellationToken);
                },
                GetPromptHandler = async (request, cancellationToken)
                    =>
                {
                    var service = request.Services!.GetRequiredService<PromptService>();
                    request.Services!.WithHeaders(headers);

                    return await service.GetServerPrompt(request.Services!, request.Server,
                        request.Params?.Name!,
                        request.Params?.Arguments ?? new Dictionary<string, JsonElement>());
                }
            }
            : null;

}