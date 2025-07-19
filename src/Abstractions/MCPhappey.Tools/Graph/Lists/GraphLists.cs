using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
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
    [McpServerTool(Name = "GraphLists_CreateListItem", Title = "Create a new Microsoft List item", ReadOnly = false, OpenWorld = false)]
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
    [McpServerTool(Name = "GraphLists_CreateList", Title = "Create a new Microsoft List", ReadOnly = false, OpenWorld = false)]
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

        var result = await mcpServer.GetElicitResponse(new GraphNewSharePointList()
        {
            Title = listTitle,
            Description = description,
            Template = template
        }, cancellationToken);

        var createdList = await client.Sites[siteId].Lists.PostAsync(
            new Microsoft.Graph.Beta.Models.List
            {
                DisplayName = result.Title,
                Description = result.Description,
                ListProp = new Microsoft.Graph.Beta.Models.ListInfo
                {
                    Template = result.Template.ToString()
                }
            },
            cancellationToken: cancellationToken
        );

        return createdList.ToJsonContentBlock(
            $"https://graph.microsoft.com/beta/sites/{siteId}/lists/{createdList.Id}");
    }

    [Description("Add a column to a Microsoft List")]
    [McpServerTool(Name = "GraphLists_AddColumn", Title = "Add a column to a Microsoft List", ReadOnly = false, OpenWorld = false)]
    public static async Task<ContentBlock?> GraphLists_AddColumn(
            [Description("ID of the SharePoint site (e.g. 'contoso.sharepoint.com,GUID,GUID')")]
        string siteId,
            [Description("ID of the Microsoft List")]
        string listId,
            [Description("Column name")]
        string columnName,
            [Description("Column display name")]
        string? columnDisplayName,
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

        var result = await mcpServer.GetElicitResponse(new GraphNewSharePointColumn()
        {
            DisplayName = columnDisplayName,
            Name = columnName,
            ColumnType = columnType,
            Choices = choices
        }, cancellationToken);

        // Build column based on type (jouw bestaande logic)
        var columnDef = GetColumnDefinition(result.Name, result.DisplayName ?? result.Name, result.ColumnType, result.Choices);

        var createdColumn = await client.Sites[siteId].Lists[listId].Columns.PostAsync(
            columnDef,
            cancellationToken: cancellationToken
        );

        return createdColumn.ToJsonContentBlock(
            $"https://graph.microsoft.com/beta/sites/{siteId}/lists/{listId}/columns/{createdColumn.Id}"
        );
    }

    [Description("Please fill in the details for the new Microsoft List.")]
    public class GraphNewSharePointList
    {
        [JsonPropertyName("title")]
        [Required]
        [Description("Name of the new list")]
        public string Title { get; set; } = default!;

        [JsonPropertyName("description")]
        [Description("Description of the list (optional)")]
        public string? Description { get; set; }

        [JsonPropertyName("template")]
        [Description("Template type for the new list")]
        [Required]
        public SharePointListTemplate Template { get; set; } = SharePointListTemplate.genericList;
    }


    [Description("Please fill in the details for the new column.")]
    public class GraphNewSharePointColumn
    {
        [JsonPropertyName("name")]
        [Required]
        [Description("Column name (no spaces, unique in list)")]
        public string Name { get; set; } = default!;

        [JsonPropertyName("displayName")]
        [Description("Column display name (optional, for UI)")]
        public string? DisplayName { get; set; }

        [JsonPropertyName("columnType")]
        [Required]
        [Description("Type of column")]
        public SharePointColumnType ColumnType { get; set; } = SharePointColumnType.Text;

        [JsonPropertyName("choices")]
        [Description("Choices (only for 'Choice' type), comma separated")]
        public string? Choices { get; set; }

        // You can add more props for Number, DateTime, etc.
        public Microsoft.Graph.Beta.Models.ColumnDefinition GetColumnDefinition()
        {
            var col = new Microsoft.Graph.Beta.Models.ColumnDefinition
            {
                Name = Name,
                DisplayName = DisplayName ?? Name
            };

            switch (ColumnType)
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
                        Choices = Choices?.Split(',').Select(x => x.Trim()).ToList() ?? new List<string>()
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

    [JsonConverter(typeof(JsonStringEnumConverter))]
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

    [JsonConverter(typeof(JsonStringEnumConverter))]
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
