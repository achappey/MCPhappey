using System.ComponentModel;
using A2A.Models;
using MCPhappey.Agent2Agent.Repositories;
using MCPhappey.Common;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using MCPhappey.Auth.Extensions;
using A2A.Server.Infrastructure;
using Microsoft.AspNetCore.Http;
using MCPhappey.Agent2Agent.Models;
using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;
using MCPhappey.Common.Extensions;

namespace MCPhappey.Agent2Agent;

public static class Agent2AgentContextPlugin
{
    [McpServerTool(Name = "Agent2AgentContext_CreateTask", ReadOnly = false)]
    [Description("Create a new Agent2Agent task.")]
    public static async Task<CallToolResult> Agent2AgentContext_CreateTask(
            IServiceProvider serviceProvider,
            RequestContext<CallToolRequestParams> requestContext,
            string contextId,
            string taskDescription,
            CancellationToken cancellationToken = default)
    {
        var tokenProvider = serviceProvider.GetRequiredService<HeaderProvider>();
        var httpContextAccessor = serviceProvider.GetRequiredService<IHttpContextAccessor>();
        var oid = tokenProvider.GetOidClaim();
        var name = tokenProvider.GetNameClaim();
        var userGroupIds = httpContextAccessor.HttpContext?.User.GetGroupClaims();

        var taskRepo = serviceProvider.GetRequiredService<IAgent2AgentTaskRepository>();
        var contextRepo = serviceProvider.GetRequiredService<IAgent2AgentContextRepository>();

        // 2. Load the context for the task
        var context = await contextRepo.GetContextAsync(contextId, cancellationToken);
        if (context == null)
            throw new UnauthorizedAccessException("Context not found");

        // 3. Check access
        var userAllowed =
            (context.UserIds != null && context.UserIds.Contains(oid)) ||
            (context.SecurityGroupIds != null && userGroupIds != null && context.SecurityGroupIds.Intersect(userGroupIds).Any());

        if (!userAllowed)
            throw new UnauthorizedAccessException("You do not have access to this task's context");




        var (typedResult, notAccepted) = await requestContext.Server.TryElicit(new NewA2ATask()
        {
            Description = taskDescription,
        }, cancellationToken);

        if (notAccepted != null) return notAccepted;
        if (typedResult == null) return "Something went wrong".ToErrorCallToolResponse();

        var taskItem = new TaskRecord()
        {
            Id = Guid.NewGuid().ToString(),
            Status = new A2A.Models.TaskStatus()
            {
                State = A2A.TaskState.Unknown,
            },
            Message = new Message()
            {
                Role = A2A.MessageRole.User,
                MessageId = Guid.NewGuid().ToString(),
                Parts = [new TextPart() {
                    Text = typedResult.Description
                }],
                Metadata = new()
                    {
                        {"timestamp",  DateTime.UtcNow.ToString("o") },
                        {"author",  name ?? requestContext.Server.ServerOptions.ServerInfo?.Name ?? string.Empty }
                    }
            }
        };

        await taskRepo.SaveTaskAsync(taskItem, cancellationToken);

        return taskItem.ToJsonContentBlock($"a2a://task/{taskItem?.Id}").ToCallToolResult();

    }

    [McpServerTool(Name = "Agent2AgentContext_CreateContext", ReadOnly = true)]
    [Description("Create a new A2A context")]
    public static async Task<CallToolResult> Agent2AgentContext_CreateContext(
       [Description("Name of the new context")] string name,
       IServiceProvider serviceProvider,
       RequestContext<CallToolRequestParams> requestContext,
       CancellationToken cancellationToken = default)
    {
        var tokenProvider = serviceProvider.GetRequiredService<HeaderProvider>();
        var oid = tokenProvider.GetOidClaim();
        ArgumentException.ThrowIfNullOrWhiteSpace(oid);

        var (typedResult, notAccepted) = await requestContext.Server.TryElicit(new NewA2AContext()
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
        [JsonPropertyName("description")]
        [Required]
        [Description("The task description.")]
        public string Description { get; set; } = default!;

    }

}

