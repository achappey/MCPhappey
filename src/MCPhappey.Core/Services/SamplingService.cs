using System.Text.Json;
using MCPhappey.Core.Extensions;
using MCPhappey.Common.Models;
using ModelContextProtocol.Protocol.Types;
using ModelContextProtocol.Server;

namespace MCPhappey.Core.Services;

public class SamplingService(PromptService promptService)
{
    public async Task<CreateMessageResult> GetPromptSample(IMcpServer mcpServer, ServerConfig serverConfig, string name,
        IReadOnlyDictionary<string, JsonElement> arguments, string? authToken = null, string? modelHint = null, float? temperature = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(serverConfig);

        var prompt = await promptService.GetServerPrompt(serverConfig, name,
            arguments, authToken,
            cancellationToken);

        return await mcpServer.RequestSamplingAsync(new CreateMessageRequestParams()
        {
            Messages = [.. prompt.Messages.Select(a => new SamplingMessage()
            {
                Role = a.Role,
                Content = a.Content
            })],
            ModelPreferences = modelHint?.ToModelPreferences(),
            Temperature = temperature
        }, cancellationToken);
    }

    public async Task<T?> GetPromptSample<T>(IMcpServer mcpServer,
        ServerConfig serverConfig,
        string name,
        IReadOnlyDictionary<string, JsonElement> arguments,
        string? authToken = null,
        string? modelHint = null,
        float? temperature = null,
        CancellationToken cancellationToken = default)
    {
        var promptSample = await GetPromptSample(mcpServer, serverConfig, name, arguments, authToken, modelHint, temperature, cancellationToken);

        return JsonSerializer.Deserialize<T>(promptSample.Content.Text?.CleanJson() ?? ""
             ?? string.Empty);
    }
}
