using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
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
    [McpServerTool(ReadOnly = false)]
    public static async Task<ContentBlock?> GraphTodo_CreateTask(
         [Description("ToDo list id")]
            string listId,
            IServiceProvider serviceProvider,
            RequestContext<CallToolRequestParams> requestContext,
            CancellationToken cancellationToken = default)
    {
        var mcpServer = requestContext.Server;
        var client = await serviceProvider.GetOboGraphClient(mcpServer);

        var elicitParams = "Please fill in the To Do task details".CreateElicitRequestParamsForType<GraphNewTodoTask>();
        var elicitResult = await requestContext.Server.ElicitAsync(elicitParams, cancellationToken: cancellationToken);
        elicitResult.EnsureAccept();

        var dto = JsonSerializer.Deserialize<GraphNewTodoTask>(JsonSerializer.Serialize(elicitResult.Content));

        var result = await client.Me.Todo.Lists[listId].Tasks
            .PostAsync(new Microsoft.Graph.Beta.Models.TodoTask
            {
                Title = dto?.Title,
                // Importance = dto?.Importance,
                DueDateTime = dto?.DueDateTime != null ? new DateTimeTimeZone
                {
                    DateTime = dto.DueDateTime?.ToString("yyyy-MM-ddTHH:mm:ss"),
                    TimeZone = "UTC"
                } : null
            }, cancellationToken: cancellationToken);

        return result.ToJsonContentBlock($"https://graph.microsoft.com/beta/me/todo/lists/${listId}/tasks");
    }

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
        [Description("Importance (low, normal, high).")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public Importance? Importance { get; set; }

    }
}