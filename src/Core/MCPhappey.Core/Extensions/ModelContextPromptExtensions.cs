using System.Text.Json;
using MCPhappey.Common.Models;
using MCPhappey.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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

                    var logger = request.Services!.GetRequiredService<ILogger<PromptsCapability>>();

                    logger.LogInformation(
                        "Action={Action} Server={Server} Prompt={Prompt}",
                        "GetPrompt",
                        serverConfig.Server.ServerInfo.Name, request.Params?.Name);

                    return await service.GetServerPrompt(request.Services!, request.Server,
                        request.Params?.Name!,
                        request.Params?.Arguments ?? new Dictionary<string, JsonElement>());
                }
            }
            : null;

}