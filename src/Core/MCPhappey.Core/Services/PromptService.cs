using System.Text.Json;
using MCPhappey.Core.Extensions;
using MCPhappey.Common.Models;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Microsoft.Extensions.DependencyInjection;
using MCPhappey.Common.Extensions;

namespace MCPhappey.Core.Services;

public class PromptService(IServerDataProvider dynamicDataService)
{
    public async Task<IEnumerable<PromptTemplate>> GetServerPromptTemplates(ServerConfig serverConfig,
             CancellationToken cancellationToken = default) => serverConfig.SourceType switch
             {
                 ServerSourceType.Static => (await Task.FromResult(serverConfig.PromptList?.Prompts)) ?? [],
                 ServerSourceType.Dynamic => await dynamicDataService.GetPromptsAsync(serverConfig.Server.ServerInfo.Name, cancellationToken) ?? [],
                 _ => await Task.FromResult(serverConfig.PromptList?.Prompts) ?? [],
             };

    public async Task<ListPromptsResult> GetServerPrompts(ServerConfig serverConfig,
          CancellationToken cancellationToken = default) => new ListPromptsResult()
          {
              Prompts = (await GetServerPromptTemplates(serverConfig, cancellationToken))
                .Select(a => a.Template)
                .OrderBy(a => a.Name)
                .ToList() ?? []
          };

    public async Task<GetPromptResult> GetServerPrompt(
        IServiceProvider serviceProvider,
        McpServer mcpServer,
        string name,
        IReadOnlyDictionary<string, JsonElement>? arguments = null,
        CancellationToken cancellationToken = default)
    {
        var serverConfig = serviceProvider.GetServerConfig(mcpServer) ?? throw new Exception();
        var prompts = await GetServerPromptTemplates(serverConfig);
        var prompt = prompts?.FirstOrDefault(a => a.Template.Name == name);

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
