using System.Text.Json;
using MCPhappey.Core.Extensions;
using MCPhappey.Common.Models;
using ModelContextProtocol.Protocol.Types;

namespace MCPhappey.Core.Services;

public class PromptService(ResourceService resourceService)
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

    public async Task<GetPromptResult> GetServerPrompt(ServerConfig serverConfig, string name,
        IReadOnlyDictionary<string, JsonElement> arguments, string? authToken = null,
        CancellationToken cancellationToken = default)
    {
        var prompt = serverConfig.PromptList?.Prompts.FirstOrDefault(a => a.Template.Name == name);

        var resourceTasks = prompt?.Resources
            ?.Select(z => resourceService.GetServerResource(serverConfig, z, authToken, cancellationToken)) ?? [];

        var resources = await Task.WhenAll(resourceTasks);
        var resourceContents = resources.SelectMany(a => a.Contents)
        .Select(a => new PromptMessage()
        {
            Role = Role.User,
            Content = new Content()
            {
                Type = "resource",
                Resource = a
            }
        }) ?? [];

        return new()
        {
            Description = prompt?.Template.Description,
            Messages = [..resourceContents,  new PromptMessage() {
                    Role = Role.User,
                    Content = new Content() {
                        Type = "text",
                        Text = prompt?.Prompt.FormatWith(arguments)
                    }
                }]
        };
    }
}
