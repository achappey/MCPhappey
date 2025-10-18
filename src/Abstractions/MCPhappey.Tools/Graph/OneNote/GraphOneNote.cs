using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using MCPhappey.Common.Extensions;
using MCPhappey.Core.Extensions;
using MCPhappey.Tools.Extensions;
using Microsoft.Graph.Beta.Models;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace MCPhappey.Tools.Graph.OneNote;

public static class GraphOneNote
{
    [Description("Create a new OneNote page in a specified section.")]
    [McpServerTool(Title = "Create OneNote page",
        Name = "graph_onenote_create_page",
        Destructive = true,
        OpenWorld = false)]
    public static async Task<CallToolResult?> GraphOneNote_CreatePage(
        [Description("The ID of the section where the page will be created.")] string sectionId,
        [Description("Title of the new page.")] string title,
        IServiceProvider serviceProvider,
        RequestContext<CallToolRequestParams> requestContext,
        CancellationToken cancellationToken = default) =>
         await requestContext.WithOboGraphClient(async client =>
    {
        var (typed, notAccepted, _) = await requestContext.Server.TryElicit(
            new GraphNewOneNotePage()
            {
                Title = title
            }, cancellationToken);
        if (notAccepted != null) return notAccepted;

        // Graph API: POST /me/onenote/sections/{sectionId}/pages
        var onenotePage = new OnenotePage()
        {
            Title = typed?.Title
        };

        // We send HTML content for the page body
        var stream = BinaryData.FromString(typed!.Content).ToStream();
        var newPage = await client.Me
            .Onenote
            .Sections[sectionId]
            .Pages
            .PostAsync(new OnenotePage()
            {
                Title = title,
            }, cancellationToken: cancellationToken);

        return newPage
            .ToJsonContentBlock($"https://graph.microsoft.com/beta/me/onenote/sections/{sectionId}/pages/{newPage?.Id}")
            .ToCallToolResult();
    });

    [Description("Create a new OneNote section in a specified notebook.")]
    [McpServerTool(Title = "Create OneNote section",
        Name = "graph_onenote_create_section",
        Destructive = true, OpenWorld = false)]
    public static async Task<CallToolResult?> GraphOneNote_CreateSection(
        [Description("The ID of the notebook where the section will be created.")] string notebookId,
        [Description("Displayname of the new section.")] string displayName,
        IServiceProvider serviceProvider,
        RequestContext<CallToolRequestParams> requestContext,
        CancellationToken cancellationToken = default) =>
         await requestContext.WithOboGraphClient(async client =>
    {
        var (typed, notAccepted, _) = await requestContext.Server.TryElicit(
            new GraphNewOneNoteSection()
            {
                DisplayName = displayName
            }, cancellationToken);
        if (notAccepted != null) return notAccepted;

        // POST /me/onenote/notebooks/{notebookId}/sections
        var section = new OnenoteSection
        {
            DisplayName = typed!.DisplayName
        };

        var newSection = await client.Me
            .Onenote
            .Notebooks[notebookId]
            .Sections
            .PostAsync(section, cancellationToken: cancellationToken);

        return (newSection ?? section)
            .ToJsonContentBlock($"https://graph.microsoft.com/beta/me/onenote/notebooks/{notebookId}/sections")
            .ToCallToolResult();
    });

    [Description("Create a new OneNote notebook for the current user.")]
    [McpServerTool(Title = "Create OneNote notebook",
        Name = "graph_onenote_create_notebook",
        Destructive = true, OpenWorld = false)]
    public static async Task<CallToolResult?> GraphOneNote_CreateNotebook(
        IServiceProvider serviceProvider,
        RequestContext<CallToolRequestParams> requestContext,
        CancellationToken cancellationToken = default) =>
        await requestContext.WithOboGraphClient(async client =>
    {
        var (typed, notAccepted, _) = await requestContext.Server.TryElicit(
            new GraphNewOneNoteNotebook(), cancellationToken);
        if (notAccepted != null) return notAccepted;

        // POST /me/onenote/notebooks
        var notebook = new Notebook
        {
            DisplayName = typed!.DisplayName
        };

        var newNotebook = await client.Me
            .Onenote
            .Notebooks
            .PostAsync(notebook, cancellationToken: cancellationToken);

        return (newNotebook ?? notebook)
            .ToJsonContentBlock($"https://graph.microsoft.com/beta/me/onenote/notebooks")
            .ToCallToolResult();
    });

    // ----- Elicited payloads -----
    [Description("Please provide details for the new OneNote page.")]
    public class GraphNewOneNotePage
    {
        [JsonPropertyName("title")]
        [Required]
        [Description("The title of the new OneNote page.")]
        public string Title { get; set; } = default!;

        [JsonPropertyName("content")]
        [Required]
        [Description("The HTML content of the new page body.")]
        public string Content { get; set; } = default!;
    }

    [Description("Please provide details for the new OneNote section.")]
    public class GraphNewOneNoteSection
    {
        [JsonPropertyName("displayName")]
        [Required]
        [Description("The name of the new section.")]
        public string DisplayName { get; set; } = default!;
    }

    [Description("Please provide details for the new OneNote notebook.")]
    public class GraphNewOneNoteNotebook
    {
        [JsonPropertyName("displayName")]
        [Required]
        [Description("The name of the new notebook.")]
        public string DisplayName { get; set; } = default!;
    }
}