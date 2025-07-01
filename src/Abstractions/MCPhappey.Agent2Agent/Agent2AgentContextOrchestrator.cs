using System.ComponentModel;
using A2A.Models;
using A2A.Server.Infrastructure;
using MCPhappey.Agent2Agent.Repositories;
using MCPhappey.Auth.Extensions;
using MCPhappey.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace MCPhappey.Agent2Agent;

public static class Agent2AgentContextOrchestrator
{
    [McpServerTool(ReadOnly = true, UseStructuredContent = true)]
    [Description("Returns all tasks associated with the specified context. Only tasks for which the current user or their security groups have access are included.")]
    public static async Task<IEnumerable<TaskRecord>> Agent2AgentContextOrchestrator_GetTasks(
      IServiceProvider serviceProvider,
      RequestContext<CallToolRequestParams> requestContext,
      string contextId,
      CancellationToken cancellationToken = default)
    {
        var repo = serviceProvider.GetRequiredService<IAgent2AgentTaskRepository>();
        var httpContextAccessor = serviceProvider.GetRequiredService<IHttpContextAccessor>();
        var contextRepo = serviceProvider.GetRequiredService<IAgent2AgentContextRepository>();
        var tokenProvider = serviceProvider.GetRequiredService<HeaderProvider>();

        var oid = tokenProvider.GetOidClaim(); // User OID

        var userGroupIds = httpContextAccessor.HttpContext?.User.GetGroupClaims();
        var context = await contextRepo.GetContextAsync(contextId, cancellationToken);

        if (context == null)
            throw new UnauthorizedAccessException("Context not found");

        // Check user access
        var userAllowed =
            (context.UserIds != null && context.UserIds.Contains(oid)) ||
            (context.SecurityGroupIds != null && userGroupIds != null && context.SecurityGroupIds.Intersect(userGroupIds).Any());

        if (!userAllowed)
            throw new UnauthorizedAccessException("You do not have access to this context");

        var tasks = await repo.GetTasksByContextAsync(contextId, cancellationToken);
        return tasks;
    }

    [McpServerTool(ReadOnly = true, UseStructuredContent = true)]
    [Description("Retrieves the specified task by ID if the current user or their security groups have access to its context.")]
    public static async Task<TaskRecord> Agent2AgentContextOrchestrator_GetTask(
     IServiceProvider serviceProvider,
     RequestContext<CallToolRequestParams> requestContext,
     string taskId,
     CancellationToken cancellationToken = default)
    {
        var tokenProvider = serviceProvider.GetRequiredService<HeaderProvider>();
        var httpContextAccessor = serviceProvider.GetRequiredService<IHttpContextAccessor>();
        var oid = tokenProvider.GetOidClaim();
        var userGroupIds = httpContextAccessor.HttpContext?.User.GetGroupClaims();

        var taskRepo = serviceProvider.GetRequiredService<IAgent2AgentTaskRepository>();
        var contextRepo = serviceProvider.GetRequiredService<IAgent2AgentContextRepository>();

        // 1. Load the task
        var task = await taskRepo.GetTaskAsync(taskId, cancellationToken);
        if (task == null)
            throw new UnauthorizedAccessException("Task not found");

        // 2. Load the context for the task
        var context = await contextRepo.GetContextAsync(task.ContextId, cancellationToken);
        if (context == null)
            throw new UnauthorizedAccessException("Context not found");

        // 3. Check access
        var userAllowed =
            (context.UserIds != null && context.UserIds.Contains(oid)) ||
            (context.SecurityGroupIds != null && userGroupIds != null && context.SecurityGroupIds.Intersect(userGroupIds).Any());

        if (!userAllowed)
            throw new UnauthorizedAccessException("You do not have access to this task's context");

        // 4. Return the task
        return task;
    }

    [McpServerTool(ReadOnly = false, UseStructuredContent = true)]
    [Description("Create a new task.")]
    public static async Task<TaskRecord> Agent2AgentContextOrchestrator_CreateTask(
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

        var taskItem = new TaskRecord()
        {
            Status = new A2A.Models.TaskStatus()
            {
                State = A2A.TaskState.Unknown,
            },
            Message = new Message()
            {
                Role = A2A.MessageRole.User,
                MessageId = Guid.NewGuid().ToString(),
                Parts = [new TextPart() {
                    Text = taskDescription
                }],
                Metadata = new()
                    {
                        {"timestamp",  DateTime.UtcNow.ToString("o") },
                        {"author",  name ?? requestContext.Server.ServerOptions.ServerInfo?.Name ?? string.Empty }
                    }
            }
        };

        await taskRepo.SaveTaskAsync(taskItem, cancellationToken);


        return taskItem;

    }

    [McpServerTool(ReadOnly = true, UseStructuredContent = true)]
    [Description("Retrieves the specified artifact by ID if the current user or their security groups have access to its context.")]
    public static async Task<Artifact?> Agent2AgentContextOrchestrator_GetArtifact(
        IServiceProvider serviceProvider,
        RequestContext<CallToolRequestParams> requestContext,
        string taskId,
        string artifactId,
        CancellationToken cancellationToken = default)
    {
        var tokenProvider = serviceProvider.GetRequiredService<HeaderProvider>();
        var httpContextAccessor = serviceProvider.GetRequiredService<IHttpContextAccessor>();
        var oid = tokenProvider.GetOidClaim();
        var userGroupIds = httpContextAccessor.HttpContext?.User.GetGroupClaims();

        var taskRepo = serviceProvider.GetRequiredService<IAgent2AgentTaskRepository>();
        var contextRepo = serviceProvider.GetRequiredService<IAgent2AgentContextRepository>();

        // 1. Load the task
        var task = await taskRepo.GetTaskAsync(taskId, cancellationToken);
        if (task == null)
            throw new UnauthorizedAccessException("Task not found");

        // 2. Load the context for the task
        var context = await contextRepo.GetContextAsync(task.ContextId, cancellationToken);
        if (context == null)
            throw new UnauthorizedAccessException("Context not found");

        // 3. Check access
        var userAllowed =
            (context.UserIds != null && context.UserIds.Contains(oid)) ||
            (context.SecurityGroupIds != null && userGroupIds != null && context.SecurityGroupIds.Intersect(userGroupIds).Any());

        if (!userAllowed)
            throw new UnauthorizedAccessException("You do not have access to this task's context");

        // 4. Return the task
        return task.Artifacts?.FirstOrDefault(a => a.ArtifactId == artifactId);

    }

}

