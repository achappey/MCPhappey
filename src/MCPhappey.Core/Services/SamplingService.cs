using System.Text.Json;
using MCPhappey.Core.Extensions;
using MCPhappey.Core.Models.Protocol;
using Microsoft.AspNetCore.Http;
using ModelContextProtocol.Protocol.Types;
using ModelContextProtocol.Server;

namespace MCPhappey.Core.Services;

public class SamplingService(PromptService promptService,
    IHttpContextAccessor httpContextAccessor,
    IReadOnlyList<ServerConfig> servers)
{
    public async Task<CreateMessageResult> GetPromptSample(IMcpServer mcpServer, string name,
        IReadOnlyDictionary<string, JsonElement> arguments, string? modelHint = null, float? temperature = null,
        CancellationToken cancellationToken = default)
    {
        var serverConfig = servers.FirstOrDefault(a => a.Server.ServerInfo.Name == mcpServer.ServerOptions.ServerInfo?.Name);
        ArgumentNullException.ThrowIfNull(serverConfig);

        var prompt = await promptService.GetServerPrompt(serverConfig.Server, name, arguments, httpContextAccessor.HttpContext!,
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

    public async Task<T?> GetPromptSample<T>(IMcpServer mcpServer, string name,
      IReadOnlyDictionary<string, JsonElement> arguments, string? modelHint = null, float? temperature = null,
      CancellationToken cancellationToken = default)
    {
        var promptSample = await GetPromptSample(mcpServer, name, arguments, modelHint, temperature, cancellationToken);

        return JsonSerializer.Deserialize<T>(promptSample.Content.Text?.CleanJson() ?? ""
             ?? string.Empty);
    }
}
