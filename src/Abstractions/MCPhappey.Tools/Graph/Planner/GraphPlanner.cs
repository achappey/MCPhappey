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
    [McpServerTool(Name = "GraphPlanner_CreateTask", ReadOnly = false, OpenWorld = false)]
    public static async Task<ContentBlock?> GraphPlanner_CreateTask(
         [Description("Planner id")]
            string plannerId,
         [Description("Bucket id")]
            string bucketId,
        [Description("New task title")]
            string title,
        IServiceProvider serviceProvider,
        RequestContext<CallToolRequestParams> requestContext,
        DateTimeOffset? dueDateTime = null,
        int? percentComplete = null,
        int? priority = null,
        CancellationToken cancellationToken = default)
    {
        var mcpServer = requestContext.Server;
        var client = await serviceProvider.GetOboGraphClient(mcpServer);
        var plan = await client.Planner.Plans[plannerId].GetAsync((config) => { }, cancellationToken);
        var bucket = await client.Planner.Plans[plannerId].Buckets[bucketId].GetAsync((config) => { }, cancellationToken);
        var dto = await requestContext.Server.GetElicitResponse(new GraphNewPlannerTask()
        {
            Title = title,
            PercentComplete = percentComplete,
            DueDateTime = dueDateTime,
            Priority = priority,
        }, cancellationToken);

        var result = await client.Planner.Tasks.PostAsync(new PlannerTask
        {
            Title = dto?.Title,
            PlanId = plannerId,
            BucketId = bucketId,
            Priority = dto?.Priority,
            PercentComplete = dto?.PercentComplete,
            DueDateTime = dto?.DueDateTime
        }, cancellationToken: cancellationToken);

        return result.ToJsonContentBlock($"https://graph.microsoft.com/beta/planner/tasks/{result.Id}");
    }

    [Description("Create a new Planner bucket in a plan")]
    [McpServerTool(Name = "GraphPlanner_CreateBucket", ReadOnly = false, OpenWorld = false)]
    public static async Task<ContentBlock?> GraphPlanner_CreateBucket(
    [Description("Planner id (plan to add bucket to)")]
        string plannerId,
    [Description("Name of the new bucket")]
        string bucketName,
    IServiceProvider serviceProvider,
    RequestContext<CallToolRequestParams> requestContext,
    [Description("Order hint for bucket placement (optional, leave empty for default).")]
        string? orderHint = null,
    CancellationToken cancellationToken = default)
    {
        var mcpServer = requestContext.Server;
        var client = await serviceProvider.GetOboGraphClient(mcpServer);
        var planner = await client.Planner.Plans[plannerId]
                               .GetAsync(cancellationToken: cancellationToken);

        var resultDto = await requestContext.Server.GetElicitResponse(new GraphNewPlannerBucket()
        {
            Name = bucketName,
            OrderHint = orderHint
        }, cancellationToken);

        var result = await client.Planner.Buckets.PostAsync(new PlannerBucket
        {
            Name = bucketName,
            PlanId = plannerId,
            OrderHint = orderHint
        }, cancellationToken: cancellationToken);

        return result.ToJsonContentBlock($"https://graph.microsoft.com/beta/planner/plans/{plannerId}/buckets");
    }

    [Description("Create a new Planner plan")]
    [McpServerTool(Name = "GraphPlanner_CreatePlan", ReadOnly = false, OpenWorld = false)]
    public static async Task<ContentBlock?> GraphPlanner_CreatePlan(
        [Description("Group id (Microsoft 365 group that will own the plan)")]
        string groupId,
        [Description("Title of the new Planner plan")]
        string planTitle,
    IServiceProvider serviceProvider,
    RequestContext<CallToolRequestParams> requestContext,
    CancellationToken cancellationToken = default)
    {
        var client = await serviceProvider.GetOboGraphClient(requestContext.Server);
        var group = await client.Groups[groupId]
                         .GetAsync(cancellationToken: cancellationToken);

        var resultDto = await requestContext.Server.GetElicitResponse(new GraphNewPlannerPlan()
        {
            Title = planTitle
        }, cancellationToken);

        var result = await client.Planner.Plans.PostAsync(new PlannerPlan
        {
            Title = resultDto.Title,
            Owner = groupId
        }, cancellationToken: cancellationToken);

        return result.ToJsonContentBlock($"https://graph.microsoft.com/beta/planner/plans/{result?.Id}");
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

    [Description("Please fill in the Planner plan details")]
    public class GraphNewPlannerPlan
    {
        [JsonPropertyName("title")]
        [Required]
        [Description("Name of the new Planner plan.")]
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