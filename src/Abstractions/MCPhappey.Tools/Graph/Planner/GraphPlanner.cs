using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using MCPhappey.Common.Extensions;
using MCPhappey.Core.Extensions;
using Microsoft.Graph.Beta.Models;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace MCPhappey.Tools.Graph.Planner;

public static partial class GraphPlanner
{
    [Description("Create a new Microsoft Planner task")]
    [McpServerTool(Name = "GraphPlanner_CreateTask", ReadOnly = false)]
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
        var dto = await requestContext.Server.GetElicitResponse<GraphNewPlannerTask>(cancellationToken);

        var client = await serviceProvider.GetOboGraphClient(mcpServer);
        var result = await client.Planner.Tasks.PostAsync(new PlannerTask
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

    [Description("Create a new Planner bucket in a plan")]
    [McpServerTool(Name = "GraphPlanner_CreateBucket", ReadOnly = false)]
    public static async Task<ContentBlock?> GraphPlanner_CreateBucket(
    [Description("Planner id (plan to add bucket to)")]
        string plannerId,
    IServiceProvider serviceProvider,
    RequestContext<CallToolRequestParams> requestContext,
    CancellationToken cancellationToken = default)
    {
        var mcpServer = requestContext.Server;
        var client = await serviceProvider.GetOboGraphClient(mcpServer);

        var dto = await requestContext.Server.GetElicitResponse<GraphNewPlannerBucket>(cancellationToken);
        var result = await client.Planner.Buckets.PostAsync(new PlannerBucket
        {
            Name = dto.Name,
            PlanId = plannerId,
            OrderHint = dto.OrderHint
        }, cancellationToken: cancellationToken);

        return result.ToJsonContentBlock("https://graph.microsoft.com/beta/planner/buckets");
    }

    [Description("Create a new Planner plan")]
    [McpServerTool(Name = "GraphPlanner_CreatePlan", ReadOnly = false)]
    public static async Task<ContentBlock?> GraphPlanner_CreatePlan(
    [Description("Group id (Microsoft 365 group that will own the plan)")]
        string groupId,
    IServiceProvider serviceProvider,
    RequestContext<CallToolRequestParams> requestContext,
    CancellationToken cancellationToken = default)
    {
        var mcpServer = requestContext.Server;
        var client = await serviceProvider.GetOboGraphClient(mcpServer);

        var dto = await requestContext.Server.GetElicitResponse<GraphNewPlannerPlan>(cancellationToken);
        var result = await client.Planner.Plans.PostAsync(new PlannerPlan
        {
            Title = dto.Title,
            Owner = groupId
        }, cancellationToken: cancellationToken);

        return result.ToJsonContentBlock("https://graph.microsoft.com/beta/planner/plans");
    }

    [Description("Please fill in the Planner plan details")]
    public class GraphNewPlannerPlan
    {
        [JsonPropertyName("title")]
        [Required]
        [Description("Name of the new Planner plan.")]
        public string Title { get; set; } = default!;
    }

    [Description("Please fill in the Planner bucket details")]
    public class GraphNewPlannerBucket
    {
        [JsonPropertyName("name")]
        [Required]
        [Description("Name of the new bucket.")]
        public string Name { get; set; } = default!;

        [JsonPropertyName("orderHint")]
        [Description("Order hint for bucket placement (optional, leave empty for default).")]
        public string? OrderHint { get; set; }
    }

    [Description("Copy Plan")]
    public class GraphCopyPlanner
    {

        [JsonPropertyName("plannerId")]
        [Required]
        [Description("The id of the original Planner to copy.")]
        public string PlannerId { get; set; } = default!;

        [JsonPropertyName("groupId")]
        [Required]
        [Description("Target group id. Where the new Planner should be created.")]
        public string GroupId { get; set; } = default!;

        [JsonPropertyName("title")]
        [Required]
        [Description("The title of the new Planner.")]
        public string Title { get; set; } = default!;

    }

    [Description("Please fill in the Planner task details")]
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