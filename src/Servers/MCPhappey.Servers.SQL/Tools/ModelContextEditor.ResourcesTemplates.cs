using System.ComponentModel;
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
    [McpServerTool(Destructive = false,
        Title = "Add a resource template to an MCP-server",
        OpenWorld = false)]
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
        [Description("Optional priority of the resource. Between 0 and 1, where 1 is most important and 0 is least important.")]
        float? priority = null,
        [Description("Optional assistant audience target.")]
        bool? assistantAudience = true,
        [Description("Optional user audience target.")]
        bool? userAudience = null,
        CancellationToken cancellationToken = default)
    {
        var serverRepository = serviceProvider.GetRequiredService<ServerRepository>();
        var server = await serviceProvider.GetServer(serverName, cancellationToken);
        var (typed, notAccepted) = await requestContext.Server.TryElicit(new AddMcpResourceTemplate()
        {
            UriTemplate = uriTemplate,
            Title = title,
            Name = name.Slugify().ToLowerInvariant(),
            AssistantAudience = assistantAudience,
            UserAudience = userAudience,
            Priority = priority,
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
            typed.Title,
            (float?)typed.Priority,
            typed.AssistantAudience,
            typed.UserAudience);

        return item.ToResourceTemplate()
                      .ToJsonContentBlock($"mcp-editor://server/{serverName}/resourceTemplates/{item.Name}")
                      .ToCallToolResult();
    }

    [Description("Updates a resource template of a MCP-server")]
    [McpServerTool(Destructive = false,
        Title = "Update a resource template of an MCP-server",
        OpenWorld = false)]
    public static async Task<CallToolResult> ModelContextEditor_UpdateResourceTemplate(
        [Description("Name of the server")] string serverName,
        [Description("Name of the resource template to update")] string resourceTemplateName,
        IServiceProvider serviceProvider,
        RequestContext<CallToolRequestParams> requestContext,
        [Description("New value for the uri template property")] string? newUriTemplate = null,
        [Description("New value for the title property")] string? newTitle = null,
        [Description("New value for the description property")] string? newDescription = null,
        [Description("New value for the priority of the resource template. Between 0 and 1, where 1 is most important and 0 is least important.")]
        float? priority = null,
        [Description("New value for the assistant audience target.")]
        bool? assistantAudience = true,
        [Description("New value for the user audience target.")]
        bool? userAudience = null,
        CancellationToken cancellationToken = default)
    {
        var serverRepository = serviceProvider.GetRequiredService<ServerRepository>();
        var server = await serviceProvider.GetServer(serverName, cancellationToken);
        var resource = server.ResourceTemplates.FirstOrDefault(a => a.Name == resourceTemplateName) ?? throw new ArgumentNullException();
        var (typed, notAccepted) = await requestContext.Server.TryElicit(new UpdateMcpResourceTemplate()
        {
            Description = newDescription ?? resource.Description,
            Title = newTitle ?? resource.Title,
            Name = resourceTemplateName,
            AssistantAudience = assistantAudience ?? resource.AssistantAudience,
            UserAudience = userAudience ?? resource.UserAudience,
            Priority = priority ?? resource.Priority,
            UriTemplate = newUriTemplate ?? resource.TemplateUri
        }, cancellationToken);

        if (notAccepted != null) return notAccepted;
        if (typed == null) return "Invalid response".ToErrorCallToolResponse();
        if (!string.IsNullOrEmpty(typed.UriTemplate))
        {
            resource.TemplateUri = typed.UriTemplate;
        }

        if (!string.IsNullOrEmpty(typed.Name))
        {
            resource.Name = typed.Name.Slugify().ToLowerInvariant();
        }

        resource.Description = typed.Description;
        resource.Title = typed.Title;
        resource.AssistantAudience = typed?.AssistantAudience;
        resource.UserAudience = typed?.UserAudience;
        resource.Priority = (float?)typed?.Priority;

        var updated = await serverRepository.UpdateResourceTemplate(resource);

        return updated.ToResourceTemplate()
                      .ToJsonContentBlock($"mcp-editor://server/{serverName}/resourceTemplates/{updated.Name}")
                      .ToCallToolResult();
    }

    [Description("Deletes a resource template from a MCP-server")]
    [McpServerTool(Title = "Delete a resource template from an MCP-server",
        OpenWorld = false)]
    public static async Task<CallToolResult> ModelContextEditor_DeleteResourceTemplate(
        [Description("Name of the server")] string serverName,
        [Description("Name of the resource template to delete")] string resourceTemplateName,
        IServiceProvider serviceProvider,
        RequestContext<CallToolRequestParams> requestContext,
        CancellationToken cancellationToken = default)
    {
        var serverRepository = serviceProvider.GetRequiredService<ServerRepository>();
        var server = await serviceProvider.GetServer(serverName, cancellationToken);

        return await requestContext.ConfirmAndDeleteAsync<ConfirmDeleteResourceTemplate>(
            expectedName: resourceTemplateName,
            deleteAction: async _ =>
            {
                var template = server.ResourceTemplates.First(z => z.Name == resourceTemplateName);
                await serverRepository.DeleteResourceTemplate(template.Id);
            },
            successText: $"Resource {resourceTemplateName} has been deleted.",
            ct: cancellationToken);
    }

}

