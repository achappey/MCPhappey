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
    [Description("List MCP-server resource templates")]
    [McpServerTool(Name = "ModelContextEditor_ListServerResourceTemplates", ReadOnly = true, OpenWorld = false)]
    public static async Task<CallToolResult> ModelContextEditor_ListServerResourceTemplates(
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

        return JsonSerializer.Serialize(server.ResourceTemplates
            .Select(a => new { UriTemplate = a.TemplateUri, a.Name, a.Description }))
            .ToTextCallToolResponse();
    }

    [Description("Adds a resource template to a MCP-server")]
    [McpServerTool(Name = "ModelContextEditor_AddResourceTemplate", OpenWorld = false)]
    public static async Task<CallToolResult> ModelContextEditor_AddResourceTemplate(
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

        if (server?.Owners.Any(a => a.Id == userId) == true)
        {
            var configs = serviceProvider.GetRequiredService<IReadOnlyList<ServerConfig>>();
            var dto = await requestContext.Server.GetElicitResponse<AddMcpResourceTemplate>(cancellationToken);
            var usedArguments = dto.UriTemplate.ExtractPromptArguments();

            if (usedArguments.Count == 0)
            {
                return "Invalid uriTemplate: No arguments found. Add arguments to the uriTemplate or create a reaource instead.".ToErrorCallToolResponse();
            }

            var config = configs.GetServerConfig(mcpServer);
            var item = await serverRepository.AddServerResourceTemplate(server.Id, dto.UriTemplate, dto.Name, dto.Description);

            return JsonSerializer.Serialize(new { UriTemplate = item.TemplateUri, item.Name, item.Description }).ToJsonCallToolResponse(dto.UriTemplate);
        }

        return "Access denied. Only owners can add resources".ToErrorCallToolResponse();
    }

    [Description("Updates a resource tempalte of a MCP-server")]
    [McpServerTool(Name = "ModelContextEditor_UpdateResourceTemplate", OpenWorld = false)]
    public static async Task<CallToolResult> ModelContextEditor_UpdateResourceTemplate(
        [Description("Name of the server")] string serverName,
        [Description("Name of the resource template to update")] string resourceTemplateName,
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

        var dto = await requestContext.Server.GetElicitResponse<UpdateMcpResourceTemplate>(cancellationToken);

        var resource = server.ResourceTemplates.FirstOrDefault(a => a.Name == resourceTemplateName) ?? throw new ArgumentNullException();

        if (!string.IsNullOrEmpty(dto.UriTemplate))
        {
            resource.TemplateUri = dto.UriTemplate;
        }

        if (!string.IsNullOrEmpty(dto.Description))
        {
            resource.Description = dto.Description;
        }

        var updated = await serverRepository.UpdateResourceTemplate(resource);

        return JsonSerializer.Serialize(new { UriTemplate = updated.TemplateUri, updated.Name, updated.Description }).ToTextCallToolResponse();
    }

    [Description("Deletes a resource template from a MCP-server")]
    [McpServerTool(Name = "ModelContextEditor_DeleteResourceTemplate", OpenWorld = false)]
    public static async Task<CallToolResult> ModelContextEditor_DeleteResourceTemplate(
        [Description("Name of the server")] string serverName,
        [Description("URI of the resource template to delete")] string resourceTemplateUri,
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

        var dto = await requestContext.Server.GetElicitResponse<ConfirmDeleteResourceTemplate>(cancellationToken);

        if (dto.UriTemplate?.Trim() != resourceTemplateUri.Trim())
            return $"Confirmation does not match URI '{resourceTemplateUri}'".ToErrorCallToolResponse();

        var resource = server.ResourceTemplates.FirstOrDefault(a => a.TemplateUri == resourceTemplateUri) ?? throw new ArgumentNullException();

        await serverRepository.DeleteResourceTemplate(resource.Id);

        return $"Resource {resourceTemplateUri} has been deleted.".ToTextCallToolResponse();
    }

    [Description("Please confirm the URI of the resource template you want to delete.")]
    public class ConfirmDeleteResourceTemplate
    {
        [JsonPropertyName("uriTemplate")]
        [Required]
        [Description("Enter the exact URI of the resource template to confirm deletion.")]
        public string UriTemplate { get; set; } = default!;
    }

    [Description("Update one or more fields. Leave blank to skip updating that field. Use a single space to clear the value.")]
    public class UpdateMcpResourceTemplate
    {
        [JsonPropertyName("uriTemplate")]
        [Description("New uri template of the resource template (optional).")]
        public string? UriTemplate { get; set; }

        [JsonPropertyName("description")]
        [Description("New description of the resource template (optional).")]
        public string? Description { get; set; }
    }

    [Description("Please fill in the details to add a new resource template to the specified MCP server.")]
    public class AddMcpResourceTemplate
    {
        [JsonPropertyName("uri")]
        [Required]
        [Description("The URI of the resource template to add.")]
        public string UriTemplate { get; set; } = default!;

        [JsonPropertyName("name")]
        [Required]
        [Description("The name of the resource template to add.")]
        public string Name { get; set; } = default!;

        [JsonPropertyName("description")]
        [Description("Optional description of the resource template.")]
        public string? Description { get; set; }
    }
}

