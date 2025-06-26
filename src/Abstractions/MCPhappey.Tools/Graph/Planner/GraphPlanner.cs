using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;
using MCPhappey.Common.Extensions;
using MCPhappey.Core.Extensions;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace MCPhappey.Tools.Graph.Planner;

public static class GraphPlanner
{
    [Description("Create a new Microsoft Planner task")]
    [McpServerTool(ReadOnly = false)]
    public static async Task<ContentBlock?> GraphPlanner_CreateTask(
         [Description("Planner id")]
            string plannerId,
         [Description("Bucket id")]
            string bucketId,
        IServiceProvider serviceProvider,
        RequestContext<CallToolRequestParams> requestContext,
        CancellationToken cancellationToken = default)
    {
        var mcpServer = requestContext.Server;
        var client = await serviceProvider.GetOboGraphClient(mcpServer);

        var elicitParams = "Please fill in the Planner task details".CreateElicitRequestParamsForType<GraphNewPlannerTask>();
        var elicitResult = await requestContext.Server.ElicitAsync(elicitParams, cancellationToken: cancellationToken);
        elicitResult.EnsureAccept();

        var dto = JsonSerializer.Deserialize<GraphNewPlannerTask>(JsonSerializer.Serialize(elicitResult.Content));
        var result = await client.Planner.Tasks.PostAsync(new Microsoft.Graph.Beta.Models.PlannerTask
        {
            Title = dto?.Title,
            PlanId = plannerId,
            BucketId = bucketId,
            Priority = dto?.Priority,
            PercentComplete = dto?.PercentComplete,
            DueDateTime = dto?.DueDateTime
        }, cancellationToken: cancellationToken);

        return result.ToJsonContentBlock("https://graph.microsoft.com/beta/planner/tasks");
    }

    public class GraphNewPlannerTask
    {
        [JsonPropertyName("title")]
        [Required]
        [Description("The task title.")]
        public string Title { get; set; } = default!;

        [JsonPropertyName("dueDateTime")]
        [Description("Due date.")]
        public DateTimeOffset? DueDateTime { get; set; }

        [JsonPropertyName("priority")]
        [Description("Priority.")]
        [Range(0, 10)]
        public int? Priority { get; set; }

        [JsonPropertyName("percentComplete")]
        [Description("Percent complete")]
        [Range(0, 100)]
        public int? PercentComplete { get; set; }
    }
}