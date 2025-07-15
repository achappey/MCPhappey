using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;
using MCPhappey.Auth.Extensions;
using MCPhappey.Common.Extensions;
using MCPhappey.Common.Models;
using MCPhappey.Core.Extensions;
using MCPhappey.Core.Services;
using MCPhappey.Servers.SQL.Repositories;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace MCPhappey.Servers.SQL.Tools;

public static partial class ModelContextEditor
{
    [Description("List MCP-server prompts")]
    [McpServerTool(Name = "ModelContextEditor_ListServerPrompts", ReadOnly = true, OpenWorld = false)]
    public static async Task<CallToolResult> ModelContextEditor_ListServerPrompts(
          [Description("Name of the server")]
            string serverName,
            IServiceProvider serviceProvider,
            CancellationToken cancellationToken = default)
    {
        var serverRepository = serviceProvider.GetRequiredService<ServerRepository>();
        var userId = serviceProvider.GetUserId();
        if (userId == null)
        {
            return "No user found".ToErrorCallToolResponse();
        }

        var server = await serverRepository.GetServer(serverName, cancellationToken);
        if (server?.Owners.Any(a => a.Id == userId) != true)
        {
            throw new UnauthorizedAccessException();
        }

        return JsonSerializer.Serialize(server.Prompts
            .Select(a => new
            {
                Prompt = a.PromptTemplate,
                a.Name,
                a.Description,
                Arguments = a.Arguments.Select(z => new { z.Name, z.Description, z.Required })
            }))
            .ToTextCallToolResponse();
    }

    [Description("Adds a prompt to a MCP-server")]
    [McpServerTool(Name = "ModelContextEditor_AddPrompt", OpenWorld = false)]
    public static async Task<CallToolResult> ModelContextEditor_AddPrompt(
        [Description("Name of the server")]
            string serverName,
        IServiceProvider serviceProvider,
        RequestContext<CallToolRequestParams> requestContext,
        IMcpServer mcpServer,
        CancellationToken cancellationToken = default)
    {
        var userId = serviceProvider.GetUserId();

        if (userId == null)
        {
            return "No user found".ToErrorCallToolResponse();
        }

        var downloadService = serviceProvider.GetRequiredService<DownloadService>();
        var serverRepository = serviceProvider.GetRequiredService<ServerRepository>();
        var server = await serverRepository.GetServer(serverName, cancellationToken);

        if (server?.Owners.Any(a => a.Id == userId) != true)
        {
            return "Access denied. Only owners can add resources".ToErrorCallToolResponse();
        }

        var configs = serviceProvider.GetRequiredService<IReadOnlyList<ServerConfig>>();
        var dto = await requestContext.Server.GetElicitResponse<AddMcpPrompt>(cancellationToken);
        var config = configs.GetServerConfig(mcpServer);
        var item = await serverRepository.AddServerPrompt(server.Id, dto.Prompt, dto.Name, dto.Description, arguments: dto.Prompt.ExtractPromptArguments().Select(a => new Models.PromptArgument()
        {
            Name = a,
            Required = true
        }));

        return JsonSerializer.Serialize(new
        {
            Prompt = item.PromptTemplate,
            item.Name,
            item.Description,
            Arguments = item.Arguments.Select(z => new { z.Name, z.Description, z.Required })
        }).ToTextCallToolResponse();

    }

    [Description("Updates a resource of a MCP-server")]
    [McpServerTool(Name = "ModelContextEditor_UpdatePrompt", OpenWorld = false)]
    public static async Task<CallToolResult> ModelContextEditor_UpdatePrompt(
    [Description("Name of the server")] string serverName,
    [Description("Name of the prompt to update")] string promptName,
    IServiceProvider serviceProvider,
    RequestContext<CallToolRequestParams> requestContext,
    CancellationToken cancellationToken = default)
    {
        var userId = serviceProvider.GetUserId();
        if (userId == null) return "No user found".ToErrorCallToolResponse();

        var serverRepository = serviceProvider.GetRequiredService<ServerRepository>();
        var server = await serverRepository.GetServer(serverName, cancellationToken);

        if (server?.Owners.Any(a => a.Id == userId) != true)
            return "Access denied.".ToErrorCallToolResponse();

        var dto = await requestContext.Server.GetElicitResponse<UpdateMcpPrompt>(cancellationToken);

        var prompt = server.Prompts.FirstOrDefault(a => a.Name == promptName) ?? throw new ArgumentNullException();
        prompt.Name = dto.Name?.Trim() == "" ? "" : dto.Name!;
        prompt.PromptTemplate = dto.Prompt?.Trim() == "" ? "" : dto.Prompt!;
        prompt.Description = dto.Description?.Trim() == "" ? "" : dto.Description;
        var usedArguments = prompt.PromptTemplate.ExtractPromptArguments();

        var toRemove = prompt.Arguments
            .Where(a => !usedArguments.Contains(a.Name, StringComparer.OrdinalIgnoreCase))
            .ToList();

        // Remove outdated arguments from EF tracking
        foreach (var arg in toRemove)
        {
            await serverRepository.DeletePromptArgument(arg.Id);
        }

        var existingNames = prompt.Arguments.Select(a => a.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var name in usedArguments)
        {
            if (!existingNames.Contains(name))
            {
                prompt.Arguments.Add(new MCPhappey.Servers.SQL.Models.PromptArgument
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
       CancellationToken cancellationToken = default)
    {
        var userId = serviceProvider.GetUserId();
        if (userId == null) return "No user found".ToErrorCallToolResponse();

        var serverRepository = serviceProvider.GetRequiredService<ServerRepository>();
        var server = await serverRepository.GetServer(serverName, cancellationToken);

        if (server?.Owners.Any(a => a.Id == userId) != true)
            return "Access denied.".ToErrorCallToolResponse();

        var prompt = server.Prompts.FirstOrDefault(a => a.Name == promptName) ?? throw new ArgumentNullException();
        var promptArgument = prompt.Arguments.FirstOrDefault(a => a.Name == promptArgumentName) ?? throw new ArgumentNullException();

        var dto = await requestContext.Server.GetElicitResponse<UpdateMcpPromptArgument>(cancellationToken);
        promptArgument.Required = dto.Required;
        promptArgument.Description = dto.Description?.Trim() == "" ? "" : dto.Description;

        var updated = await serverRepository.UpdatePromptArgument(promptArgument);

        return JsonSerializer
            .Serialize(new { updated.Name, updated.Description, updated.Required })
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
        var userId = serviceProvider.GetUserId();
        if (userId == null) return "No user found".ToErrorCallToolResponse();

        var serverRepository = serviceProvider.GetRequiredService<ServerRepository>();
        var server = await serverRepository.GetServer(serverName, cancellationToken);

        if (server?.Owners.Any(a => a.Id == userId) != true)
            return "Access denied.".ToErrorCallToolResponse();

        var dto = await requestContext.Server.GetElicitResponse<ConfirmDeletePrompt>(cancellationToken);

        if (dto.Name?.Trim() != promptName.Trim())
            return $"Confirmation does not match name '{promptName}'".ToErrorCallToolResponse();

        var prompt = server.Prompts.FirstOrDefault(z => z.Name == promptName) ?? throw new ArgumentException();
        await serverRepository.DeletePrompt(prompt.Id);

        return $"Prompt {promptName} has been deleted.".ToTextCallToolResponse();
    }

    [Description("Please confirm the name of the prompt you want to delete.")]
    public class ConfirmDeletePrompt
    {
        [JsonPropertyName("name")]
        [Required]
        [Description("Enter the exact name of the prompt to confirm deletion.")]
        public string Name { get; set; } = default!;
    }

    [Description("Update one or more fields. Leave blank to skip updating that field. Use a single space to clear the value.")]
    public class UpdateMcpPrompt
    {
        [JsonPropertyName("prompt")]
        [Description("New prompt (optional).")]
        public string? Prompt { get; set; }

        [JsonPropertyName("name")]
        [Description("New name of the prompt (optional).")]
        public string? Name { get; set; }

        [JsonPropertyName("description")]
        [Description("New description of the prompt (optional).")]
        public string? Description { get; set; }
    }

    [Description("Update one or more fields. Leave blank to skip updating that field. Use a single space to clear the value.")]
    public class UpdateMcpPromptArgument
    {
        [JsonPropertyName("description")]
        [Description("New description of the prompt argument (optional).")]
        public string? Description { get; set; }

        [JsonPropertyName("required")]
        [Description("If the argument is required (optional).")]
        public bool? Required { get; set; }
    }

    [Description("Please fill in the details to add a new prompt to the specified MCP server.")]
    public class AddMcpPrompt
    {
        [JsonPropertyName("prompt")]
        [Required]
        [Description("The prompt to add")]
        public string Prompt { get; set; } = default!;

        [JsonPropertyName("name")]
        [Required]
        [Description("The name of the resource to add.")]
        public string Name { get; set; } = default!;

        [JsonPropertyName("description")]
        [Description("Optional description of the resource.")]
        public string? Description { get; set; }
    }
}

