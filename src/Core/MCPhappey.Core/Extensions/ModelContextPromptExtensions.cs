using System.Text.Json;
using MCPhappey.Common.Models;
using MCPhappey.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Protocol;

namespace MCPhappey.Core.Extensions;

public static partial class ModelContextPromptExtensions
{
    public static async Task<ListPromptsResult?> ToListPromptsResult(this ServerConfig serverConfig,
       ModelContextProtocol.Server.RequestContext<ListPromptsRequestParams> request,
        CancellationToken cancellationToken = default)
    {
        var service = request.Services!.GetRequiredService<PromptService>();

        return serverConfig.Server.Capabilities.Prompts != null ?
            await service.GetServerPrompts(serverConfig, cancellationToken)
          : null;
    }

    public static async Task<GetPromptResult>? ToGetPromptResult(
        this ModelContextProtocol.Server.RequestContext<GetPromptRequestParams> request,
        Dictionary<string, string>? headers = null,
        CancellationToken cancellationToken = default)
    {
        var service = request.Services!.GetRequiredService<PromptService>();

        request.Services!.WithHeaders(headers);

        return await service.GetServerPrompt(request.Services!, request.Server,
            request.Params?.Name!,
            request.Params?.Arguments ?? new Dictionary<string, JsonElement>(),
            cancellationToken);

    }
}