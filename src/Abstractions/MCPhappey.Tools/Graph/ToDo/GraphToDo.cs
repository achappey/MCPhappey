using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using MCPhappey.Common.Extensions;
using MCPhappey.Core.Extensions;
using Microsoft.Graph.Beta.Models;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace MCPhappey.Tools.Graph.ToDo;

public static class GraphToDo
{
    [Description("Create a new Microsoft To Do task")]
    [McpServerTool(Title = "Create Microsoft To Do task", Destructive = false,
        OpenWorld = false)]
    public static async Task<CallToolResult?> GraphTodo_CreateTask(
     [Description("ToDo list id")] string listId,
     IServiceProvider serviceProvider,
     RequestContext<CallToolRequestParams> requestContext,
     [Description("The task title.")] string? title = null,
     [Description("The due date (YYYY-MM-DD).")] DateTime? dueDateTime = null,
     [Description("Importance (low, normal, high).")] Importance? importance = null,
     CancellationToken cancellationToken = default)
    {
        var mcpServer = requestContext.Server;
        using var client = await serviceProvider.GetOboGraphClient(mcpServer);

        var (typed, notAccepted) = await mcpServer.TryElicit(
            new GraphNewTodoTask
            {
                Title = title ?? string.Empty,
                DueDateTime = dueDateTime,
                Importance = importance
            },
            cancellationToken
        );

        if (notAccepted != null) return notAccepted;

        var result = await client.Me.Todo.Lists[listId].Tasks
            .PostAsync(new TodoTask
            {
                Title = typed?.Title,
                Importance = typed?.Importance,
                DueDateTime = typed?.DueDateTime != null ? new DateTimeTimeZone
                {
                    DateTime = typed.DueDateTime?.ToString("yyyy-MM-ddTHH:mm:ss"),
                    TimeZone = "UTC"
                } : null
            }, cancellationToken: cancellationToken);

        return result.ToJsonContentBlock($"https://graph.microsoft.com/beta/me/todo/lists/{listId}/tasks")
            .ToCallToolResult();
    }

    [Description("Create a new Microsoft To Do task list")]
    [McpServerTool(Name = "GraphTodo_CreateTaskList",
        Title = "Create Microsoft To Do task list",
        Destructive = false,
        OpenWorld = false)]
    public static async Task<CallToolResult?> GraphTodo_CreateTaskList(
     [Description("The task display name.")] string displayName,
     IServiceProvider serviceProvider,
     RequestContext<CallToolRequestParams> requestContext,
     CancellationToken cancellationToken = default)
    {
        var mcpServer = requestContext.Server;
        using var client = await serviceProvider.GetOboGraphClient(mcpServer);

        var (typed, notAccepted) = await mcpServer.TryElicit<GraphNewTodoTaskList>(
            new GraphNewTodoTaskList
            {
                DisplayName = displayName ?? string.Empty,
            },
            cancellationToken
        );

        if (notAccepted != null) return notAccepted;

        var result = await client.Me.Todo.Lists
            .PostAsync(new TodoTaskList
            {
                DisplayName = typed?.DisplayName,
            }, cancellationToken: cancellationToken);

        return result.ToJsonContentBlock($"https://graph.microsoft.com/beta/me/todo/lists/{result?.Id}")
            .ToCallToolResult();
    }

    [Description("Please fill in the To Do task details")]
    public class GraphNewTodoTask
    {
        [JsonPropertyName("title")]
        [Required]
        [Description("The task title.")]
        public string Title { get; set; } = default!;

        [JsonPropertyName("dueDateTime")]
        [Description("The due date (YYYY-MM-DD).")]
        public DateTime? DueDateTime { get; set; }

        [JsonPropertyName("importance")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        [Description("Importance (low, normal, high).")]
        public Importance? Importance { get; set; }

    }

    [Description("Please fill in the To Do task list details")]
    public class GraphNewTodoTaskList
    {
        [JsonPropertyName("displayName")]
        [Required]
        [Description("The task display name.")]
        public string DisplayName { get; set; } = default!;
    }
}