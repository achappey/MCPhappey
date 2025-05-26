using System.Text.Json;
using MCPhappey.Core.Extensions;
using MCPhappey.Common.Models;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Microsoft.Extensions.DependencyInjection;

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
        IReadOnlyDictionary<string, JsonElement> arguments,
        CancellationToken cancellationToken = default)
    {
        var serverConfig = serviceProvider.GetServerConfig(mcpServer) ?? throw new Exception();
        var prompt = serverConfig.PromptList?.Prompts.FirstOrDefault(a => a.Template.Name == name);

        ArgumentNullException.ThrowIfNull(prompt);
        prompt.Template.ValidatePrompt(arguments);

        var resourceService = serviceProvider.GetRequiredService<ResourceService>();

        var resourceTask = GetResourceMessagesAsync(serviceProvider, resourceService, mcpServer, serverConfig, prompt, cancellationToken);
        var templateTask = GetResourceTemplateMessagesAsync(serviceProvider, resourceService, mcpServer, serverConfig, prompt, arguments, cancellationToken);

        await Task.WhenAll(resourceTask, templateTask);

        var resourceMessages = resourceTask.Result;
        var templateMessages = templateTask.Result;

        var promptMessage = new PromptMessage
        {
            Role = Role.User,
            Content = new Content
            {
                Type = "text",
                Text = prompt?.Prompt.FormatPrompt(prompt.Template, arguments)
            }
        };

        return new GetPromptResult
        {
            Description = prompt?.Template.Description,
            Messages = [.. resourceMessages, .. templateMessages, promptMessage]
        };
    }

    private static async Task<List<PromptMessage>> GetResourceMessagesAsync(
        IServiceProvider serviceProvider,
        ResourceService resourceService,
        IMcpServer mcpServer,
        ServerConfig serverConfig,
        PromptTemplate prompt,
        CancellationToken cancellationToken)
    {
        var resourceTasks = prompt?.Resources?
            .Select(z => resourceService.GetServerResource(serviceProvider, mcpServer, serverConfig, z, cancellationToken))
            ?? Enumerable.Empty<Task<ReadResourceResult>>();

        var resources = await Task.WhenAll(resourceTasks);

        return resources
            .SelectMany(a => a.Contents)
            .Select(a => new PromptMessage
            {
                Role = Role.User,
                Content = new Content
                {
                    Type = "resource",
                    Resource = a
                }
            })
            .ToList();
    }

    // Helper: fetches resource templates (parallel)
    private static async Task<List<PromptMessage>> GetResourceTemplateMessagesAsync(
        IServiceProvider serviceProvider,
        ResourceService resourceService,
        IMcpServer mcpServer,
        ServerConfig serverConfig,
        PromptTemplate prompt,
        IReadOnlyDictionary<string, JsonElement> arguments,
        CancellationToken cancellationToken)
    {
        var resourceTemplateTasks = prompt?.ResourceTemplates?
            .Select(a => a.FormatPrompt(prompt.Template, arguments))
            .Where(a => a.CountPromptArguments() == 0)
            .Select(z => resourceService.GetServerResource(serviceProvider, mcpServer, serverConfig, z, cancellationToken))
            ?? Enumerable.Empty<Task<ReadResourceResult>>();

        var resources = await Task.WhenAll(resourceTemplateTasks);

        return resources
            .SelectMany(a => a.Contents)
            .Select(a => new PromptMessage
            {
                Role = Role.User,
                Content = new Content()
                {
                    Type = "resource",
                    Resource = a
                }
            })
            .ToList();
    }
}
