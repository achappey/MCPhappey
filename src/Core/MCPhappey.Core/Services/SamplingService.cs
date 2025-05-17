using System.Text.Json;
using MCPhappey.Core.Extensions;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace MCPhappey.Core.Services;

public class SamplingService(PromptService promptService)
{
    public async Task<CreateMessageResult> GetPromptSample(IServiceProvider serviceProvider, 
        IMcpServer mcpServer, string name,
        IReadOnlyDictionary<string, JsonElement> arguments, string? modelHint = null, float? temperature = null,
        CancellationToken cancellationToken = default)
    {
        var prompt = await promptService.GetServerPrompt(serviceProvider, mcpServer, name,
            arguments, cancellationToken);

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

    public async Task<T?> GetPromptSample<T>(IServiceProvider serviceProvider,
        IMcpServer mcpServer,
        string name,
        IReadOnlyDictionary<string, JsonElement> arguments,
        string? modelHint = null,
        float? temperature = null,
        CancellationToken cancellationToken = default)
    {
        var promptSample = await GetPromptSample(serviceProvider, mcpServer, name, arguments, modelHint, temperature, cancellationToken);

        return JsonSerializer.Deserialize<T>(promptSample.Content.Text?.CleanJson() ?? ""
             ?? string.Empty);
    }
}
