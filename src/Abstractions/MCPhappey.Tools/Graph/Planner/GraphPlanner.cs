using System.ComponentModel;
using MCPhappey.Common.Extensions;
using MCPhappey.Core.Extensions;
using MCPhappey.Tools.Graph.Planner.Models;
using Microsoft.Graph.Beta.Models;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace MCPhappey.Tools.Graph.Planner;

public static partial class GraphPlanner
{
    [Description("Create a new Microsoft Planner task")]
    [McpServerTool(Name = "GraphPlanner_CreateTask", Title = "Create a new Microsoft Planner task", ReadOnly = false, OpenWorld = false)]
    public static async Task<CallToolResult?> GraphPlanner_CreateTask(
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
        using var client = await serviceProvider.GetOboGraphClient(mcpServer);
        var plan = await client.Planner.Plans[plannerId].GetAsync((config) => { }, cancellationToken);
        var bucket = await client.Planner.Plans[plannerId].Buckets[bucketId].GetAsync((config) => { }, cancellationToken);
        var (typed, notAccepted) = await requestContext.Server.TryElicit<GraphNewPlannerTask>(
            new GraphNewPlannerTask
            {
                Title = title,
                PercentComplete = percentComplete,
                DueDateTime = dueDateTime,
                Priority = priority
            },
            cancellationToken
        );

        if (notAccepted != null) return notAccepted;

        var result = await client.Planner.Tasks.PostAsync(new PlannerTask
        {
            Title = typed?.Title,
            PlanId = plannerId,
            BucketId = bucketId,
            Priority = typed?.Priority,
            PercentComplete = typed?.PercentComplete,
            DueDateTime = typed?.DueDateTime
        }, cancellationToken: cancellationToken);

        return result.ToJsonContentBlock($"https://graph.microsoft.com/beta/planner/tasks/{result.Id}").ToCallToolResult();
    }


    [Description("Create a new Planner bucket in a plan")]
    [McpServerTool(Name = "GraphPlanner_CreateBucket", Title = "Create a new Planner bucket in a plan", ReadOnly = false, OpenWorld = false)]
    public static async Task<CallToolResult?> GraphPlanner_CreateBucket(
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
        using var client = await serviceProvider.GetOboGraphClient(mcpServer);
        var planner = await client.Planner.Plans[plannerId]
                               .GetAsync(cancellationToken: cancellationToken);

        var (typed, notAccepted) = await requestContext.Server.TryElicit<GraphNewPlannerBucket>(new GraphNewPlannerBucket()
        {
            Name = bucketName,
            OrderHint = orderHint
        }, cancellationToken);

        if (notAccepted != null) return notAccepted;
        if (typed == null) return "Invalid result".ToErrorCallToolResponse();

        var result = await client.Planner.Buckets.PostAsync(new PlannerBucket
        {
            Name = typed.Name,
            PlanId = plannerId,
            OrderHint = typed.OrderHint
        }, cancellationToken: cancellationToken);

        return result.ToJsonContentBlock($"https://graph.microsoft.com/beta/planner/plans/{plannerId}/buckets").ToCallToolResult();
    }

    [Description("Create a new Planner plan")]
    [McpServerTool(Name = "GraphPlanner_CreatePlan", Title = "Create a new Planner plan", ReadOnly = false, OpenWorld = false)]
    public static async Task<CallToolResult?> GraphPlanner_CreatePlan(
            [Description("Group id (Microsoft 365 group that will own the plan)")]
        string groupId,
            [Description("Title of the new Planner plan")]
        string planTitle,
        IServiceProvider serviceProvider,
        RequestContext<CallToolRequestParams> requestContext,
        CancellationToken cancellationToken = default)
    {
        using var client = await serviceProvider.GetOboGraphClient(requestContext.Server);
        var group = await client.Groups[groupId]
                         .GetAsync(cancellationToken: cancellationToken);

        var (typed, notAccepted) = await requestContext.Server.TryElicit<GraphNewPlannerPlan>(
            new GraphNewPlannerPlan
            {
                Title = planTitle
            },
            cancellationToken
        );

        if (notAccepted != null) return notAccepted;
        if (typed == null) return "Invalid result".ToErrorCallToolResponse();

        var result = await client.Planner.Plans.PostAsync(new PlannerPlan
        {
            Title = typed.Title,
            Owner = groupId
        }, cancellationToken: cancellationToken);

        return result.ToJsonContentBlock($"https://graph.microsoft.com/beta/planner/plans/{result?.Id}").ToCallToolResult();
    }

}