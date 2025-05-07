using System.Text.Json;
using MCPhappey.Core.Extensions;
using MCPhappey.Core.Models.Protocol;
using Microsoft.AspNetCore.Http;
using ModelContextProtocol.Protocol.Types;

namespace MCPhappey.Core.Services;

public class PromptService(ResourceService resourceService,
    IReadOnlyList<ServerConfig> servers)
{
    public async Task<ListPromptsResult> GetServerPrompts(Server server,
        CancellationToken cancellationToken = default)
    {
        var promptList = servers
                         .FirstOrDefault(a => a.Server.ServerInfo.Name
                             .Equals(server.ServerInfo.Name, StringComparison.OrdinalIgnoreCase))?.PromptList
                         ?? new();

        return await Task.FromResult<ListPromptsResult>(new()
        {
            Prompts = [.. promptList.Prompts.Select(z => z.Template)]
        });
    }

    public async Task<GetPromptResult> GetServerPrompt(Server server, string name,
        IReadOnlyDictionary<string, JsonElement> arguments, HttpContext httpContext,
        CancellationToken cancellationToken = default)
    {
        var promptList = servers
                         .FirstOrDefault(a => a.Server.ServerInfo.Name
                             .Equals(server.ServerInfo.Name, StringComparison.OrdinalIgnoreCase))?.PromptList;

        var prompt = promptList?.Prompts.FirstOrDefault(a => a.Template.Name == name);

        var resourceTasks = prompt?.Resources
            ?.Select(z => resourceService.GetServerResource(server, z, httpContext)) ?? [];

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
