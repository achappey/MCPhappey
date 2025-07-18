using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using MCPhappey.Common.Extensions;
using MCPhappey.Core.Extensions;
using MCPhappey.Tools.Extensions;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace MCPhappey.Tools.Graph.Lists;

public static class GraphLists
{
    [Description("Create a new Microsoft List item")]
    [McpServerTool(Name = "GraphLists_CreateListItem", ReadOnly = false, OpenWorld = false)]
    public static async Task<ContentBlock?> GraphLists_CreateListItem(
        string siteId,            // ID of the SharePoint site
        string listId,            // ID of the Microsoft List
        IServiceProvider serviceProvider,
        RequestContext<CallToolRequestParams> requestContext,
        CancellationToken cancellationToken = default)
    {
        var mcpServer = requestContext.Server;
        var client = await serviceProvider.GetOboGraphClient(mcpServer);

        var list = await client
              .Sites[siteId]
              .Lists[listId].GetAsync(cancellationToken: cancellationToken);

        var columns = await client
               .Sites[siteId]
               .Lists[listId]
               .Columns
               .GetAsync(cancellationToken: cancellationToken);

        ElicitRequestParams.RequestSchema request = new()
        {
            Required = []
        };

        var definitionColumns = columns?.Value?.Where(col => col.Name != "ID" && col.ReadOnly != true)
            .ToDictionary(a => a.Name!, a => new { def = a.ToElicitSchemaDef(), req = a.Required })
            .Where(a => a.Value.def != null);

        foreach (var col in definitionColumns ?? [])
        {
            request.Properties.Add(col.Key, col.Value.def!);

            if (col.Value.req == true)
            {
                request.Required.Add(col.Key);
            }
        }

        var elicitResult = await mcpServer.ElicitAsync(new ElicitRequestParams()
        {
            RequestedSchema = request,
            Message = list?.DisplayName ?? list?.Name ?? "New SharePoint list item"
        }, cancellationToken: cancellationToken);

        elicitResult.EnsureAccept();

        var values = elicitResult.Content;
        var fieldsPayload = new Dictionary<string, object>();

        foreach (var field in request.Properties)
        {
            if (values!.TryGetValue(field.Key, out var val) && val.ToString() != null)
                fieldsPayload[field.Key] = val;
        }

        var createdItem = await client
            .Sites[siteId]
            .Lists[listId]
            .Items
            .PostAsync(new Microsoft.Graph.Beta.Models.ListItem
            {
                Fields = new Microsoft.Graph.Beta.Models.FieldValueSet
                {
                    AdditionalData = fieldsPayload
                }
            }, cancellationToken: cancellationToken);

        // 6. Return success/result (customize as needed):
        return JsonSerializer.Serialize(createdItem)
            .ToJsonContentBlock($"https://graph.microsoft.com/beta/sites/{siteId}/lists/{listId}/items/{createdItem.Id}");
    }

    [Description("Create a new Microsoft List")]
    [McpServerTool(Name = "GraphLists_CreateList", ReadOnly = false, OpenWorld = false)]
    public static async Task<ContentBlock?> GraphLists_CreateList(
        [Description("ID of the SharePoint site (e.g. 'contoso.sharepoint.com,GUID,GUID')")]
        string siteId,
        [Description("Title of the new list")]
        string listTitle,
        IServiceProvider serviceProvider,
        RequestContext<CallToolRequestParams> requestContext,
        [Description("Title of the new list")]
        SharePointListTemplate template = SharePointListTemplate.genericList,
        [Description("Description of the new list")]
        string? description = null,
        CancellationToken cancellationToken = default)
    {
        var mcpServer = requestContext.Server;
        var client = await serviceProvider.GetOboGraphClient(mcpServer);

        var site = await client.Sites[siteId]
                             .GetAsync(cancellationToken: cancellationToken);

        // Elicit details for the new list
        var values = new Dictionary<string, string>
        {
            { "Site", site?.DisplayName ?? site?.WebUrl ?? string.Empty },
            { "Title", listTitle },
            { "Description", description ?? string.Empty },
            { "Template", template.ToString() }
        };

        // AI-native: Elicit message alleen voor confirm/review
        await mcpServer.GetElicitResponse(values, cancellationToken);

        var createdList = await client.Sites[siteId].Lists.PostAsync(
            new Microsoft.Graph.Beta.Models.List
            {
                DisplayName = listTitle,
                Description = description,
                ListProp = new Microsoft.Graph.Beta.Models.ListInfo
                {
                    Template = template.ToString()
                }
            },
            cancellationToken: cancellationToken
        );

        return createdList.ToJsonContentBlock(
            $"https://graph.microsoft.com/beta/sites/{siteId}/lists/{createdList.Id}");
    }

    [Description("Add a column to a Microsoft List")]
    [McpServerTool(Name = "GraphLists_AddColumn", ReadOnly = false, OpenWorld = false)]
    public static async Task<ContentBlock?> GraphLists_AddColumn(
        [Description("ID of the SharePoint site (e.g. 'contoso.sharepoint.com,GUID,GUID')")]
        string siteId,
        [Description("ID of the Microsoft List")]
        string listId,
        [Description("Column name")]
        string columnName,
        [Description("Column description")]
        string? description,
        IServiceProvider serviceProvider,
        RequestContext<CallToolRequestParams> requestContext,
        [Description("Column type (e.g. text, number, boolean, dateTime, choice)")]
        SharePointColumnType columnType = SharePointColumnType.Text,
        [Description("Choices values. Comma seperated list.")]
        string? choices = null,
        CancellationToken cancellationToken = default)
    {
        var mcpServer = requestContext.Server;
        var client = await serviceProvider.GetOboGraphClient(mcpServer);

        var site = await client
                      .Sites[siteId]
                      .GetAsync(cancellationToken: cancellationToken);

        var list = await client
            .Sites[siteId]
            .Lists[listId]
            .GetAsync(cancellationToken: cancellationToken);

        // Parameters samenvoegen tot dictionary
        var values = new Dictionary<string, string>
        {
            { "Site", site?.DisplayName ?? site?.WebUrl ?? string.Empty },
            { "List", list?.DisplayName ?? string.Empty },
            { "Column name", columnName },
            { "Column type", columnType.ToString()! },
            { "Description", description ?? string.Empty },
            { "Choices", choices ?? string.Empty }
        };

        // Elicit confirm/review (geen dubbele input)
        await mcpServer.GetElicitResponse(values, cancellationToken);

        // Build column based on type (jouw bestaande logic)
        var columnDef = GetColumnDefinition(columnName, description ?? string.Empty, columnType, choices);

        var createdColumn = await client.Sites[siteId].Lists[listId].Columns.PostAsync(
            columnDef,
            cancellationToken: cancellationToken
        );

        return createdColumn.ToJsonContentBlock(
            $"https://graph.microsoft.com/beta/sites/{siteId}/lists/{listId}/columns/{createdColumn.Id}"
        );
    }

    public static Microsoft.Graph.Beta.Models.ColumnDefinition GetColumnDefinition(string name, string displayName, SharePointColumnType columnType, string? choices = null)
    {
        var col = new Microsoft.Graph.Beta.Models.ColumnDefinition
        {
            Name = name,
            DisplayName = displayName ?? name
        };

        switch (columnType)
        {
            case SharePointColumnType.Text:
                col.Text = new Microsoft.Graph.Beta.Models.TextColumn();
                break;
            case SharePointColumnType.Number:
                col.Number = new Microsoft.Graph.Beta.Models.NumberColumn();
                break;
            case SharePointColumnType.YesNo:
                col.Boolean = new Microsoft.Graph.Beta.Models.BooleanColumn();
                break;
            case SharePointColumnType.Choice:
                col.Choice = new Microsoft.Graph.Beta.Models.ChoiceColumn
                {
                    Choices = choices?.Split(',').Select(x => x.Trim()).ToList() ?? new List<string>()
                };
                break;
            case SharePointColumnType.DateTime:
                col.DateTime = new Microsoft.Graph.Beta.Models.DateTimeColumn();
                break;
            // Add more types as needed
            default:
                throw new NotImplementedException("Unsupported column type");
        }

        return col;
    }

    public enum SharePointColumnType
    {
        [Description("Text (single line)")]
        Text,
        [Description("Number")]
        Number,
        [Description("Yes/No (boolean)")]
        YesNo,
        [Description("Choice (dropdown)")]
        Choice,
        [Description("Date/Time")]
        DateTime
        // Add more as needed
    }

    public enum SharePointListTemplate
    {
        [Description("Custom list (genericList)")]
        genericList,

        [Description("Document library (documentLibrary)")]
        [JsonPropertyName("documentLibrary")]
        documentLibrary,

        [Description("Task list (tasks)")]
        [JsonPropertyName("tasks")]
        tasks,

        [Description("Issue tracking (issues)")]
        [JsonPropertyName("issues")]
        issues,

        [Description("Calendar (events)")]
        [JsonPropertyName("events")]
        events

        // Add more if needed
    }

}
