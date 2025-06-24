using System.Text.Json;
using MCPhappey.Common.Extensions;
using MCPhappey.Core.Extensions;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace MCPhappey.Core.Services;

public class SamplingService(PromptService promptService)
{
    public async Task<CreateMessageResult> GetPromptSample(IServiceProvider serviceProvider,
        IMcpServer mcpServer, string name,
        IReadOnlyDictionary<string, JsonElement> arguments, string? modelHint = null,
        float? temperature = null,
        string? systemPrompt = null,
        ContextInclusion includeContext = ContextInclusion.None,
        CancellationToken cancellationToken = default)
    {
        var prompt = await promptService.GetServerPrompt(serviceProvider, mcpServer, name,
            arguments, cancellationToken);

        return await mcpServer.SampleAsync(new CreateMessageRequestParams()
        {
            Messages = [.. prompt.Messages.Select(a => new SamplingMessage()
            {
                Role = a.Role,
                Content = a.Content
            })],
            IncludeContext = includeContext,
            MaxTokens = 4096,
            SystemPrompt = systemPrompt,
            ModelPreferences = modelHint?.ToModelPreferences(),
            Temperature = temperature
        });
    }

    public async Task<T?> GetPromptSample<T>(IServiceProvider serviceProvider,
        IMcpServer mcpServer,
        string name,
        IReadOnlyDictionary<string, JsonElement> arguments,
        string? modelHint = null,
        float? temperature = null,
         string? systemPrompt = null,
        ContextInclusion includeContext = ContextInclusion.None,
        CancellationToken cancellationToken = default)
    {
        var promptSample = await GetPromptSample(serviceProvider, mcpServer, name, arguments, modelHint,
            temperature, systemPrompt, includeContext, cancellationToken);

        try
        {
            return JsonSerializer.Deserialize<T>(promptSample.ToText()?.CleanJson()!);

        }
        catch (JsonException exception)
        {

            var prompt = await promptService.GetServerPrompt(serviceProvider, mcpServer, name,
            arguments, cancellationToken);

            var newResult = await mcpServer.SampleAsync(new CreateMessageRequestParams()
            {
                Messages = [.. prompt.Messages.Select(a => new SamplingMessage()
            {
                Role = a.Role,
                Content = a.Content
            }),
            new SamplingMessage() {
                    Role = Role .User,
                    Content = new TextContentBlock() {
                        Text = $"Your last answer failed to JsonSerializer.Deserialize. Error message is included. Please try again.\n\n{exception.Message}"
                    }
            }],
                IncludeContext = includeContext,
                MaxTokens = 4096,
                SystemPrompt = systemPrompt,
                ModelPreferences = modelHint?.ToModelPreferences(),
                Temperature = temperature
            });

            return JsonSerializer.Deserialize<T>(newResult.ToText()?.CleanJson()!);
        }

    }
}
