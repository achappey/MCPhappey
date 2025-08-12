using System.ComponentModel;
using MCPhappey.Agent2Agent.Repositories;
using MCPhappey.Common;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using MCPhappey.Auth.Extensions;
using Microsoft.AspNetCore.Http;
using MCPhappey.Agent2Agent.Models;
using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;
using MCPhappey.Common.Extensions;
using System.Text.Json;

namespace MCPhappey.Agent2Agent;

public static class Agent2AgentContextPlugin
{
    [McpServerTool(Name = "agent2agent_new_task")]
    [Description("Execute an Agent2Agent task.")]
    public static async Task<CallToolResult> Agent2Agent_NewTask(
          IServiceProvider serviceProvider,
          RequestContext<CallToolRequestParams> requestContext,
         [Description("Url of the agent")] string agentUrl,
         [Description("Task message/comment")] string taskMessage,
         [Description("Id of the context to execute the task in. Leave empty for new context.")] string? contextId = null,
         CancellationToken cancellationToken = default)
    {
        var tokenProvider = serviceProvider.GetRequiredService<HeaderProvider>();
        var httpContextAccessor = serviceProvider.GetRequiredService<IHttpContextAccessor>();
        var oid = tokenProvider.GetOidClaim();
        var userGroupIds = httpContextAccessor.HttpContext?.User.GetGroupClaims();

        var contextRepo = serviceProvider.GetRequiredService<IAgent2AgentContextRepository>();

        // 2. Load the context for the task
        if (contextId != null)
        {
            var context = await contextRepo.GetContextAsync(contextId, cancellationToken);
            if (context == null)
                return "Context not found".ToErrorCallToolResponse();

            var userAllowed =
                (context.UserIds != null && context.UserIds.Contains(oid)) ||
                (context.SecurityGroupIds != null && userGroupIds != null && context.SecurityGroupIds.Intersect(userGroupIds).Any());

            if (!userAllowed)
                return "You do not have access to this task's context".ToErrorCallToolResponse();
        }

        // 3. Check access
        var (typedResult, notAccepted, result) = await requestContext.Server.TryElicit(new NewA2ATask()
        {
            Message = taskMessage,
        }, cancellationToken);

        if (notAccepted != null) return notAccepted;
        if (typedResult == null) return "Something went wrong".ToErrorCallToolResponse();

        return new CallToolResult()
        {
            Content = [new TextContentBlock() {
                Text = JsonSerializer.Serialize(result, JsonSerializerOptions.Web)
            }]
        };
    }

    [McpServerTool(Name = "agent2agent_send_message")]
    [Description("Execute an existing Agent2Agent task with a new message.")]
    public static async Task<CallToolResult> Agent2Agent_SendMessage(
          IServiceProvider serviceProvider,
          RequestContext<CallToolRequestParams> requestContext,
         [Description("Url of the agent")] string agentUrl,
         [Description("Id of the task to send the message to")] string taskId,
         [Description("Task message/comment")] string taskMessage,
         CancellationToken cancellationToken = default)
    {
        var tokenProvider = serviceProvider.GetRequiredService<HeaderProvider>();
        var httpContextAccessor = serviceProvider.GetRequiredService<IHttpContextAccessor>();
        var oid = tokenProvider.GetOidClaim();
        var name = tokenProvider.GetNameClaim();
        var userGroupIds = httpContextAccessor.HttpContext?.User.GetGroupClaims();

        var taskRepo = serviceProvider.GetRequiredService<IAgent2AgentTaskRepository>();
        var contextRepo = serviceProvider.GetRequiredService<IAgent2AgentContextRepository>();
        var currentTask = await taskRepo.GetTaskAsync(taskId, cancellationToken);

        // 2. Load the context for the task
        if (currentTask?.ContextId != null)
        {
            var context = await contextRepo.GetContextAsync(currentTask.ContextId, cancellationToken);
            if (context == null)
                return "Context not found".ToErrorCallToolResponse();

            var userAllowed =
                (context.UserIds != null && context.UserIds.Contains(oid)) ||
                (context.SecurityGroupIds != null && userGroupIds != null && context.SecurityGroupIds.Intersect(userGroupIds).Any());

            if (!userAllowed)
                return "You do not have access to this task's context".ToErrorCallToolResponse();
        }

        // 3. Check access
        var (typedResult, notAccepted, result) = await requestContext.Server.TryElicit(new NewA2ATask()
        {
            Message = taskMessage,
        }, cancellationToken);

        if (notAccepted != null) return notAccepted;
        if (typedResult == null) return "Something went wrong".ToErrorCallToolResponse();

        return new CallToolResult()
        {
            Content = [new TextContentBlock() {
                Text = JsonSerializer.Serialize(result, JsonSerializerOptions.Web)
            }]
        };
    }

    [McpServerTool(Name = "agent2agent_update_context", OpenWorld = false)]
    [Description("Update an existing A2A context")]
    public static async Task<CallToolResult> Agent2Agent_UpdateContext(
        [Description("ID of the context to update")] string contextId,
        IServiceProvider serviceProvider,
        RequestContext<CallToolRequestParams> requestContext,
        [Description("New name of the context")] string? newName = null,
        CancellationToken cancellationToken = default)
    {
        var tokenProvider = serviceProvider.GetRequiredService<HeaderProvider>();
        var oid = tokenProvider.GetOidClaim();
        ArgumentException.ThrowIfNullOrWhiteSpace(oid);

        // Elicit update details (name/metadata) via TryElicit, just like with Create
        var (typedResult, notAccepted, result) = await requestContext.Server.TryElicit(new UpdateA2AContext()
        {
            Name = newName
        }, cancellationToken);

        if (notAccepted != null) return notAccepted;
        if (typedResult == null) return "Something went wrong".ToErrorCallToolResponse();

        var repo = serviceProvider.GetRequiredService<IAgent2AgentContextRepository>();
        var context = await repo.GetContextAsync(contextId, cancellationToken);
        if (context == null)
            return $"Context {contextId} not found".ToErrorCallToolResponse();

        if (!context.OwnerIds.Contains(oid))
            return "You are not allowed to update this context".ToErrorCallToolResponse();

        if (!string.IsNullOrWhiteSpace(typedResult.Name))
            context.Metadata["name"] = typedResult.Name;

        await repo.SaveContextAsync(context, cancellationToken);

        return context.ToJsonContentBlock($"a2a://context/{context.ContextId}").ToCallToolResult();
    }

    [Description("Provide updated values for the context.")]
    public class UpdateA2AContext
    {
        [JsonPropertyName("name")]
        [Required]
        [Description("The new context name.")]
        public string? Name { get; set; }
    }

    [McpServerTool(Name = "agent2agent_create_context", OpenWorld = false)]
    [Description("Create a new A2A context")]
    public static async Task<CallToolResult> Agent2Agent_CreateContext(
       [Description("Name of the new context")] string name,
       IServiceProvider serviceProvider,
       RequestContext<CallToolRequestParams> requestContext,
       CancellationToken cancellationToken = default)
    {
        var tokenProvider = serviceProvider.GetRequiredService<HeaderProvider>();
        var oid = tokenProvider.GetOidClaim();
        ArgumentException.ThrowIfNullOrWhiteSpace(oid);

        var (typedResult, notAccepted, result) = await requestContext.Server.TryElicit(new NewA2AContext()
        {
            Name = name,
        }, cancellationToken);

        if (notAccepted != null) return notAccepted;
        if (typedResult == null) return "Something went wrong".ToErrorCallToolResponse();
        var repo = serviceProvider.GetRequiredService<IAgent2AgentContextRepository>();

        var newContext = new Agent2AgentContext()
        {
            ContextId = Guid.NewGuid().ToString(),
            Metadata = new Dictionary<string, object>() { { "name", typedResult.Name },
                { "createdAt", DateTimeOffset.UtcNow } },
            OwnerIds = [oid],
            UserIds = [oid]
        };

        await repo.SaveContextAsync(newContext, cancellationToken);
        return newContext.ToJsonContentBlock($"a2a://context/{newContext.ContextId}").ToCallToolResult();
    }


    [Description("Please fill in the context details.")]
    public class NewA2AContext
    {
        [JsonPropertyName("name")]
        [Required]
        [Description("The context name.")]
        public string Name { get; set; } = default!;

    }

    [Description("Please fill in the task details.")]
    public class NewA2ATask
    {
        [JsonPropertyName("message")]
        [Required]
        [Description("The task message.")]
        public string Message { get; set; } = default!;

    }

}

