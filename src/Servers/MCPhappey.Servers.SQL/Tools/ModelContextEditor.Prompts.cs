using System.ComponentModel;
using System.Text.Json;
using MCPhappey.Common.Extensions;
using MCPhappey.Core.Extensions;
using MCPhappey.Servers.SQL.Extensions;
using MCPhappey.Servers.SQL.Repositories;
using MCPhappey.Servers.SQL.Tools.Models;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace MCPhappey.Servers.SQL.Tools;

public static partial class ModelContextEditor
{

    [Description("Adds a prompt to a MCP-server")]
    [McpServerTool(Name = "ModelContextEditor_AddPrompt", ReadOnly = false, OpenWorld = false)]
    public static async Task<CallToolResult> ModelContextEditor_AddPrompt(
        [Description("Name of the server")]
            string serverName,
        [Description("The name of the prompt to add")]
            string promptName,
        [Description("The prompt to add. You can use {argument} style placeholders for prompt arguments.")]
            string prompt,
        IServiceProvider serviceProvider,
        RequestContext<CallToolRequestParams> requestContext,
        [Description("Optional title of the prompt.")]
        string? title = null,
        [Description("Optional description of the prompt.")]
        string? description = null,
        CancellationToken cancellationToken = default)
    {
        var server = await serviceProvider.GetServer(serverName, cancellationToken);
        var (typed, notAccepted) = await requestContext.Server.TryElicit(new AddMcpPrompt()
        {
            Name = promptName.Slugify().ToLowerInvariant(),
            Prompt = prompt,
            Title = title,
            Description = description
        }, cancellationToken);
        if (notAccepted != null) return notAccepted;
        if (typed == null) return "Invalid response".ToErrorCallToolResponse();

        var serverRepository = serviceProvider.GetRequiredService<ServerRepository>();
        var item = await serverRepository.AddServerPrompt(server.Id, typed.Prompt,
            typed.Name,
            typed.Description,
            typed.Title,
            arguments: typed.Prompt.ExtractPromptArguments().Select(a => new SQL.Models.PromptArgument()
            {
                Name = a,
                Required = true
            }));

        return JsonSerializer.Serialize(new
        {
            Prompt = item.PromptTemplate,
            item.Name,
            item.Title,
            item.Description,
            Arguments = item.Arguments.Select(z => z.ToPromptArgument())
        }).ToTextCallToolResponse();

    }

    [Description("Updates a resource of a MCP-server")]
    [McpServerTool(Name = "ModelContextEditor_UpdatePrompt", OpenWorld = false)]
    public static async Task<CallToolResult> ModelContextEditor_UpdatePrompt(
        [Description("Name of the server")] string serverName,
        [Description("Name of the prompt to update")] string promptName,
        IServiceProvider serviceProvider,
        RequestContext<CallToolRequestParams> requestContext,
        [Description("New value for the prompt property")] string? newPrompt = null,
        [Description("New value for the title property")] string? newTitle = null,
        [Description("New value for the description property")] string? newDescription = null,
        CancellationToken cancellationToken = default)
    {
        var serverRepository = serviceProvider.GetRequiredService<ServerRepository>();
        var server = await serviceProvider.GetServer(serverName, cancellationToken);
        var prompt = server.Prompts.FirstOrDefault(a => a.Name == promptName) ?? throw new ArgumentNullException();
        var (typed, notAccepted) = await requestContext.Server.TryElicit(new UpdateMcpPrompt()
        {
            Prompt = newPrompt ?? prompt.PromptTemplate,
            Description = newDescription ?? prompt.Description,
            Title = newTitle ?? prompt.Title,
        }, cancellationToken);
        if (notAccepted != null) return notAccepted;
        if (typed == null) return "Invalid response".ToErrorCallToolResponse();

        prompt.Description = typed.Description;
        prompt.Title = typed.Title;

        if (!string.IsNullOrEmpty(typed.Prompt))
        {
            prompt.PromptTemplate = typed.Prompt;
        }

        var usedArguments = prompt.PromptTemplate.ExtractPromptArguments();
        var toRemove = prompt.Arguments
            .Where(a => !usedArguments.Contains(a.Name, StringComparer.OrdinalIgnoreCase))
            .ToList();

        foreach (var arg in toRemove)
        {
            await serverRepository.DeletePromptArgument(arg.Id);
        }

        var existingNames = prompt.Arguments.Select(a => a.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var name in usedArguments)
        {
            if (!existingNames.Contains(name))
            {
                prompt.Arguments.Add(new SQL.Models.PromptArgument
                {
                    Name = name,
                    Required = true // default
                });
            }
        }

        var updated = await serverRepository.UpdatePrompt(prompt);

        return JsonSerializer.Serialize(new
        {
            Prompt = updated.PromptTemplate,
            updated.Name,
            updated.Title,
            updated.Description,
            Arguments = updated.Arguments.Select(z => new { z.Name, z.Description, z.Required })
        }).ToTextCallToolResponse();
    }

    [Description("Updates a prompt argument of a MCP-server")]
    [McpServerTool(Name = "ModelContextEditor_UpdatePromptArgument", OpenWorld = false)]
    public static async Task<CallToolResult> ModelContextEditor_UpdatePromptArgument(
       [Description("Name of the server")] string serverName,
       [Description("Name of the prompt to update")] string promptName,
       [Description("Name of the prompt argument to update")] string promptArgumentName,
       IServiceProvider serviceProvider,
       RequestContext<CallToolRequestParams> requestContext,
       [Description("New value for the prompt required property")] bool? required = null,
       [Description("New value for the prompt description property")] string? newDescription = null,
       CancellationToken cancellationToken = default)
    {
        var server = await serviceProvider.GetServer(serverName, cancellationToken);
        var prompt = server.Prompts.FirstOrDefault(a => a.Name == promptName) ?? throw new ArgumentNullException(nameof(promptName));
        var promptArgument = prompt.Arguments.FirstOrDefault(a => a.Name == promptArgumentName) ?? throw new ArgumentNullException(nameof(promptArgumentName));
        var (typed, notAccepted) = await requestContext.Server.TryElicit(new UpdateMcpPromptArgument()
        {
            Required = required ?? promptArgument.Required,
            Description = newDescription ?? promptArgument.Description
        }, cancellationToken);

        if (notAccepted != null) return notAccepted;
        if (typed == null) return "Invalid response".ToErrorCallToolResponse();

        promptArgument.Required = typed.Required;
        promptArgument.Description = typed.Description;

        var serverRepository = serviceProvider.GetRequiredService<ServerRepository>();
        var updated = await serverRepository.UpdatePromptArgument(promptArgument);

        return JsonSerializer
            .Serialize(updated.ToPromptArgument())
            .ToTextCallToolResponse();
    }

    [Description("Deletes a prompt from a MCP-server")]
    [McpServerTool(Name = "ModelContextEditor_DeletePrompt", OpenWorld = false)]
    public static async Task<CallToolResult> ModelContextEditor_DeletePrompt(
        [Description("Name of the server")] string serverName,
        [Description("Name of the prompt to delete")] string promptName,
        IServiceProvider serviceProvider,
        RequestContext<CallToolRequestParams> requestContext,
        CancellationToken cancellationToken = default)
    {
        var serverRepository = serviceProvider.GetRequiredService<ServerRepository>();
        var server = await serviceProvider.GetServer(serverName, cancellationToken);
        var dto = await requestContext.Server.GetElicitResponse<ConfirmDeletePrompt>(cancellationToken);
        var notAccepted = dto?.NotAccepted();
        if (notAccepted != null) return notAccepted;
        var typed = dto?.GetTypedResult<ConfirmDeletePrompt>() ?? throw new Exception();

        if (typed.Name?.Trim() != promptName.Trim())
            return $"Confirmation does not match name '{promptName}'".ToErrorCallToolResponse();

        var prompt = server.Prompts.FirstOrDefault(z => z.Name == promptName) ?? throw new ArgumentException();
        await serverRepository.DeletePrompt(prompt.Id);

        return $"Prompt {promptName} deleted.".ToTextCallToolResponse();
    }
}

