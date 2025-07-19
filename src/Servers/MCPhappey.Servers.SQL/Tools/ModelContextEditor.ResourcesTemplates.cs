using System.ComponentModel;
using System.Text.Json;
using MCPhappey.Auth.Extensions;
using MCPhappey.Common.Extensions;
using MCPhappey.Core.Extensions;
using MCPhappey.Servers.SQL.Extensions;
using MCPhappey.Servers.SQL.Repositories;
using MCPhappey.Servers.SQL.Tools.Models;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Std;

namespace MCPhappey.Servers.SQL.Tools;

public static partial class ModelContextEditor
{
    [Description("Adds a resource template to a MCP-server")]
    [McpServerTool(Name = "ModelContextEditor_AddResourceTemplate", OpenWorld = false)]
    public static async Task<CallToolResult> ModelContextEditor_AddResourceTemplate(
        [Description("Name of the server")]
        string serverName,
        [Description("The URI template of the resource template to add.")]
        string uriTemplate,
        [Description("The name of the resource template to add.")]
        string name,
        IServiceProvider serviceProvider,
        RequestContext<CallToolRequestParams> requestContext,
        [Description("Optional description of the resource template.")]
        string? description = null,
        CancellationToken cancellationToken = default)
    {
        var serverRepository = serviceProvider.GetRequiredService<ServerRepository>();
        var server = await serviceProvider.GetServer(serverName, cancellationToken);
        var dto = await requestContext.Server.GetElicitResponse(new AddMcpResourceTemplate()
        {
            UriTemplate = uriTemplate,
            Name = name,
            Description = description
        }, cancellationToken);

        var usedArguments = dto.UriTemplate.ExtractPromptArguments();

        if (usedArguments.Count == 0)
        {
            return "Invalid uriTemplate: No arguments found. Add arguments to the uriTemplate or create a reaource instead.".ToErrorCallToolResponse();
        }

        var item = await serverRepository.AddServerResourceTemplate(server.Id, dto.UriTemplate, dto.Name, dto.Description);

        return JsonSerializer.Serialize(item.ToResourceTemplate()).ToJsonCallToolResponse(dto.UriTemplate);
    }

    [Description("Updates a resource template of a MCP-server")]
    [McpServerTool(Name = "ModelContextEditor_UpdateResourceTemplate", OpenWorld = false)]
    public static async Task<CallToolResult> ModelContextEditor_UpdateResourceTemplate(
        [Description("Name of the server")] string serverName,
        [Description("Name of the resource template to update")] string resourceTemplateName,
        IServiceProvider serviceProvider,
        RequestContext<CallToolRequestParams> requestContext,
       [Description("New value for the uri template property")] string? newUriTemplate = null,
       [Description("New value for the description property")] string? newDescription = null,

        CancellationToken cancellationToken = default)
    {
        var serverRepository = serviceProvider.GetRequiredService<ServerRepository>();
        var server = await serviceProvider.GetServer(serverName, cancellationToken);
        var dto = await requestContext.Server.GetElicitResponse(new UpdateMcpResourceTemplate()
        {
            Description = newDescription,
            UriTemplate = newUriTemplate
        }, cancellationToken);

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

        return JsonSerializer.Serialize(updated.ToResourceTemplate())
            .ToJsonCallToolResponse(updated.TemplateUri);
    }

    [Description("Deletes a resource template from a MCP-server")]
    [McpServerTool(Name = "ModelContextEditor_DeleteResourceTemplate", OpenWorld = false)]
    public static async Task<CallToolResult> ModelContextEditor_DeleteResourceTemplate(
        [Description("Name of the server")] string serverName,
        [Description("Name of the resource template to delete")] string resourceTemplateName,
        IServiceProvider serviceProvider,
        RequestContext<CallToolRequestParams> requestContext,
        CancellationToken cancellationToken = default)
    {
        var serverRepository = serviceProvider.GetRequiredService<ServerRepository>();
        var server = await serviceProvider.GetServer(serverName, cancellationToken);
        var dto = await requestContext.Server.GetElicitResponse<ConfirmDeleteResourceTemplate>(cancellationToken);

        if (dto.Name?.Trim() != resourceTemplateName.Trim())
            return $"Confirmation does not match name '{resourceTemplateName}'".ToErrorCallToolResponse();

        var resource = server.ResourceTemplates.FirstOrDefault(a => a.Name == resourceTemplateName) ?? throw new ArgumentNullException();

        await serverRepository.DeleteResourceTemplate(resource.Id);

        return $"Resource {resourceTemplateName} has been deleted.".ToTextCallToolResponse();
    }
}

