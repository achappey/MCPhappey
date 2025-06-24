using System.Text.Json;
using MCPhappey.Core.Extensions;
using MCPhappey.Common.Models;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Microsoft.Extensions.DependencyInjection;
using MCPhappey.Common.Extensions;

namespace MCPhappey.Core.Services;

public class PromptService
{
    public async Task<ListPromptsResult> GetServerPrompts(ServerConfig serverConfig,
        CancellationToken cancellationToken = default)
    {
        var promptList = serverConfig.PromptList
                         ?? new();

        return await Task.FromResult<ListPromptsResult>(new()
        {
            Prompts = [.. promptList.Prompts.Select(z => z.Template)]
        });
    }

    public async Task<GetPromptResult> GetServerPrompt(
        IServiceProvider serviceProvider,
        IMcpServer mcpServer,
        string name,
        IReadOnlyDictionary<string, JsonElement>? arguments = null)
    {
        var serverConfig = serviceProvider.GetServerConfig(mcpServer) ?? throw new Exception();
        var prompt = serverConfig.PromptList?.Prompts.FirstOrDefault(a => a.Template.Name == name);

        ArgumentNullException.ThrowIfNull(prompt);
        prompt.Template.ValidatePrompt(arguments);

        var resourceService = serviceProvider.GetRequiredService<ResourceService>();
        var promptMessage = new PromptMessage
        {
            Role = Role.User,
            Content = prompt?.Prompt
                .FormatPrompt(prompt.Template, arguments)!
                .ToTextContentBlock()!
        };

        return await Task.FromResult(new GetPromptResult
        {
            Description = prompt?.Template.Description,
            Messages = [promptMessage]
        });
    }
}
