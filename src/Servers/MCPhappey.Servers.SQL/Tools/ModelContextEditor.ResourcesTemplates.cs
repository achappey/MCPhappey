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
        [Description("Optional title of the resource template.")]
        string? title = null,
        [Description("Optional description of the resource template.")]
        string? description = null,
        CancellationToken cancellationToken = default)
    {
        var serverRepository = serviceProvider.GetRequiredService<ServerRepository>();
        var server = await serviceProvider.GetServer(serverName, cancellationToken);
        var (typed, notAccepted) = await requestContext.Server.TryElicit(new AddMcpResourceTemplate()
        {
            UriTemplate = uriTemplate,
            Title = title,
            Name = name.Slugify().ToLowerInvariant(),
            Description = description
        }, cancellationToken);
        if (notAccepted != null) return notAccepted;
        if (typed == null) return "Invalid response".ToErrorCallToolResponse();
        var usedArguments = typed.UriTemplate.ExtractPromptArguments();

        if (usedArguments.Count == 0)
        {
            return "Invalid uriTemplate: No arguments found. Add arguments to the uriTemplate or create a reaource instead.".ToErrorCallToolResponse();
        }

        var item = await serverRepository.AddServerResourceTemplate(server.Id,
            typed.UriTemplate,
            typed.Name.Slugify().ToLowerInvariant(),
            typed.Description,
            typed.Title);

        return JsonSerializer.Serialize(item.ToResourceTemplate()).ToJsonCallToolResponse(typed.UriTemplate);
    }

    [Description("Updates a resource template of a MCP-server")]
    [McpServerTool(Name = "ModelContextEditor_UpdateResourceTemplate", OpenWorld = false)]
    public static async Task<CallToolResult> ModelContextEditor_UpdateResourceTemplate(
        [Description("Name of the server")] string serverName,
        [Description("Name of the resource template to update")] string resourceTemplateName,
        IServiceProvider serviceProvider,
        RequestContext<CallToolRequestParams> requestContext,
        [Description("New value for the uri template property")] string? newUriTemplate = null,
        [Description("New value for the title property")] string? newTitle = null,
        [Description("New value for the description property")] string? newDescription = null,
        CancellationToken cancellationToken = default)
    {
        var serverRepository = serviceProvider.GetRequiredService<ServerRepository>();
        var server = await serviceProvider.GetServer(serverName, cancellationToken);
        var resource = server.ResourceTemplates.FirstOrDefault(a => a.Name == resourceTemplateName) ?? throw new ArgumentNullException();
        var (typed, notAccepted) = await requestContext.Server.TryElicit(new UpdateMcpResourceTemplate()
        {
            Description = newDescription ?? resource.Description,
            Title = newTitle ?? resource.Title,
            UriTemplate = newUriTemplate ?? resource.TemplateUri
        }, cancellationToken);

        if (notAccepted != null) return notAccepted;
        if (typed == null) return "Invalid response".ToErrorCallToolResponse();
        if (!string.IsNullOrEmpty(typed.UriTemplate))
        {
            resource.TemplateUri = typed.UriTemplate;
        }

        resource.Description = typed.Description;
        resource.Title = typed.Title;

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
        var notAccepted = dto?.NotAccepted();
        if (notAccepted != null) return notAccepted;
        var typed = dto?.GetTypedResult<ConfirmDeleteResourceTemplate>() ?? throw new Exception();

        if (typed.Name?.Trim() != resourceTemplateName.Trim())
            return $"Confirmation does not match name '{resourceTemplateName}'".ToErrorCallToolResponse();

        var resource = server.ResourceTemplates.FirstOrDefault(a => a.Name == resourceTemplateName) ?? throw new ArgumentNullException();

        await serverRepository.DeleteResourceTemplate(resource.Id);

        return $"Resource {resourceTemplateName} has been deleted.".ToTextCallToolResponse();
    }
}

