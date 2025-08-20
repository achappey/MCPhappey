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
using MCPhappey.Common.Models;
using A2A.Models;
using MCPhappey.Core.Extensions;
using A2A.Server.Infrastructure;

namespace MCPhappey.Agent2Agent;

public static class Agent2AgentContextPlugin
{
    [McpServerTool(Name = "agent2agent_get_authenticated_card",
        Title = "Read A2A Agent card", OpenWorld = false, Destructive = false)]
    [Description("Get a complete A2A Agent card")]
    public static async Task<CallToolResult> Agent2Agent_GetAuthenticatedCard(
          IServiceProvider serviceProvider,
          RequestContext<CallToolRequestParams> requestContext,
          string agentUrl,
          CancellationToken cancellationToken = default) => await System.Threading.Tasks.Task.FromResult(new CallToolResult());

    [McpServerTool(Name = "agent2agent_create_task", Title = "Create Agent2Agent task", OpenWorld = false)]
    [Description("Create a new Agent2Agent task without executing it. Make sure you add all required and optional, but needed, extensions")]
    public static async Task<CallToolResult> Agent2Agent_CreateTask(
           IServiceProvider serviceProvider,
           RequestContext<CallToolRequestParams> requestContext,
           string contextId,
           string taskDescription,
           [Description("Agent extension to use for the task.")] List<Models.AgentExtension> extensions,
           [Description("Comma seperated list of referenced task ids.")] string? referencedTaskIds = null,
           CancellationToken cancellationToken = default)
    {
        if (extensions == null)
        {
            return "Extensions are missing. Please carefully read the Agent card and task requirements and add the needed extensions. If no extensionsa are needed, sent in an empty object".ToErrorCallToolResponse();
        }

        var tokenProvider = serviceProvider.GetRequiredService<HeaderProvider>();
        var httpContextAccessor = serviceProvider.GetRequiredService<IHttpContextAccessor>();
        var oid = tokenProvider.GetOidClaim();
        var name = tokenProvider.GetNameClaim();
        var userGroupIds = httpContextAccessor.HttpContext?.User.GetGroupClaims();

        var taskRepo = serviceProvider.GetRequiredService<IAgent2AgentTaskRepository>();
        var contextRepo = serviceProvider.GetRequiredService<IAgent2AgentContextRepository>();
        var refTaskIds = referencedTaskIds?.Split(",") ?? [];

        await System.Threading.Tasks.Task.WhenAll(refTaskIds.Select(t => taskRepo.GetTaskAsync(t, cancellationToken)));

        // 2. Load the context for the task
        var context = await contextRepo.GetContextAsync(contextId, cancellationToken)
            ?? throw new UnauthorizedAccessException("Context not found");

        // 3. Check access
        var userAllowed =
            (context.UserIds != null && context.UserIds.Contains(oid)) ||
            (context.SecurityGroupIds != null && userGroupIds != null && context.SecurityGroupIds.Intersect(userGroupIds).Any());

        if (!userAllowed)
            throw new UnauthorizedAccessException("You do not have access to this task's context");

        var (typedResult, notAccepted, result) = await requestContext.Server.TryElicit(new NewA2ATask()
        {
            Message = taskDescription,
        }, cancellationToken);

        if (notAccepted != null) return notAccepted;
        if (typedResult == null) return "Something went wrong".ToErrorCallToolResponse();

        var meta = new Dictionary<string, object>
            {
                { "timestamp", DateTime.UtcNow.ToString("o") },
                { "author", name ?? requestContext.Server.ServerOptions.ServerInfo?.Name ?? string.Empty }
            };

        // Only keys: uri → true
        if (extensions != null)
        {
            foreach (var ext in extensions)
            {
                if (!string.IsNullOrWhiteSpace(ext.Uri.ToString()))
                    meta[ext.Uri.ToString()] = new { }; // value can be true/null/whatever you want
            }
        }

        var taskItem = new TaskRecord()
        {
            Id = Guid.NewGuid().ToString(),
            ContextId = contextId,
            Status = new A2A.Models.TaskStatus()
            {
                State = A2A.TaskState.Unknown,
            },
            Message = new Message()
            {
                Role = A2A.MessageRole.User,
                MessageId = Guid.NewGuid().ToString(),
                Parts = [new TextPart() {
                    Text = typedResult.Message
                }],
                ReferenceTaskIds = [.. refTaskIds],
                Metadata = [.. meta]
            }
        };

        await taskRepo.SaveTaskAsync(taskItem, cancellationToken);

        return taskItem.ToJsonContentBlock($"a2a://task/{taskItem?.Id}").ToCallToolResult();
    }

    [McpServerTool(Name = "agent2agent_new_task", Title = "Execute new Agent2Agent task")]
    [Description("Create a new task and execute it by the specified agent")]
    public static async Task<CallToolResult> Agent2Agent_NewTask(
          IServiceProvider serviceProvider,
          RequestContext<CallToolRequestParams> requestContext,
         [Description("Url of the agent")] string agentUrl,
         [Description("Id of the context to execute the task in.")] string contextId,
         [Description("Task message/comment")] string taskMessage,
         [Description("Agent extension to use for the task.")] List<Models.AgentExtension> extensions,
         [Description("Comma seperated list of referenced task ids.")] string? referencedTaskIds = null,
         CancellationToken cancellationToken = default)
    {
        if (extensions == null)
        {
            return "Extensions are missing. Please carefulle read the Agent card and task requirements and add the needed extensions. If no extensionsa are needed, sent in an emty object".ToErrorCallToolResponse();
        }

        var tokenProvider = serviceProvider.GetRequiredService<HeaderProvider>();
        var httpContextAccessor = serviceProvider.GetRequiredService<IHttpContextAccessor>();
        var oid = tokenProvider.GetOidClaim();
        var userGroupIds = httpContextAccessor.HttpContext?.User.GetGroupClaims();
        var taskRepo = serviceProvider.GetRequiredService<IAgent2AgentTaskRepository>();
        var contextRepo = serviceProvider.GetRequiredService<IAgent2AgentContextRepository>();

        // 2. Load the context for the task
        if (contextId != null)
        {
            var context = await contextRepo.GetContextAsync(contextId, cancellationToken);
            if (context == null)
                return "Context not found. Please create a new context first or use an existing context.".ToErrorCallToolResponse();

            var userAllowed =
                (context.UserIds != null && context.UserIds.Contains(oid)) ||
                (context.SecurityGroupIds != null && userGroupIds != null && context.SecurityGroupIds.Intersect(userGroupIds).Any());

            if (!userAllowed)
                return "You do not have access to this task's context".ToErrorCallToolResponse();
        }

        var refTaskIds = referencedTaskIds?.Split(",") ?? [];
        await System.Threading.Tasks.Task.WhenAll(refTaskIds.Select(t => taskRepo.GetTaskAsync(t, cancellationToken)));

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

    [McpServerTool(Name = "agent2agent_send_message", Title = "Send Agent2Agent task message")]
    [Description("Execute an existing Agent2Agent task with a new message.")]
    public static async Task<CallToolResult> Agent2Agent_SendMessage(
          IServiceProvider serviceProvider,
          RequestContext<CallToolRequestParams> requestContext,
         [Description("Url of the agent")] string agentUrl,
         [Description("Id of the task to send the message to")] string taskId,
         [Description("Task message/comment")] string taskMessage,
         [Description("Comma seperated list of referenced task ids.")] string? referencedTaskIds = null,
         CancellationToken cancellationToken = default)
    {
        var tokenProvider = serviceProvider.GetRequiredService<HeaderProvider>();
        var httpContextAccessor = serviceProvider.GetRequiredService<IHttpContextAccessor>();
        var oid = tokenProvider.GetOidClaim();
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
        var refTaskIds = referencedTaskIds?.Split(",") ?? [];
        await System.Threading.Tasks.Task.WhenAll(refTaskIds.Select(t => taskRepo.GetTaskAsync(t, cancellationToken)));
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

    [Description("Delete an A2A context")]
    [McpServerTool(Name = "agent2agent_delete_context", Title = "Delete an A2A context",
        OpenWorld = false)]
    public static async Task<CallToolResult> Agent2Agent_DeleteContext(
        [Description("Id of the context")] string contextId,
        RequestContext<CallToolRequestParams> requestContext,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken = default)
    {
        var taskRepo = serviceProvider.GetRequiredService<IAgent2AgentTaskRepository>();
        var oid = serviceProvider.GetUserId();
        var contextRepo = serviceProvider.GetRequiredService<IAgent2AgentContextRepository>();
        var context = await contextRepo.GetContextAsync(contextId, cancellationToken);

        if (context == null)
            return "Context not found".ToErrorCallToolResponse();

        var userAllowed =
            context.OwnerIds != null && context.OwnerIds.Contains(oid);

        if (!userAllowed)
            return "You do not have access to this task's context".ToErrorCallToolResponse();

        // One-liner again
        return await requestContext.ConfirmAndDeleteAsync<DeleteA2AContext>(
            context.Metadata.TryGetValue("name", out object? value) ? value.ToString()! : context.ContextId,
            async _ => await contextRepo.DeleteContextAsync(contextId, cancellationToken),
            "Context deleted.",
            cancellationToken);
    }

    [Description("Upload task artifacts to users' OneDrive")]
    [McpServerTool(Name = "agent2agent_upload_artifacts",
        Title = "Upload artifacts to OneDrive",
        OpenWorld = false)]
    public static async Task<CallToolResult> Agent2Agent_UploadArtifacts(
       [Description("Id of the task")] string taskId,
       RequestContext<CallToolRequestParams> requestContext,
       IServiceProvider serviceProvider,
       CancellationToken cancellationToken = default)
    {
        var taskRepo = serviceProvider.GetRequiredService<IAgent2AgentTaskRepository>();
        var oid = serviceProvider.GetUserId();
        var contextRepo = serviceProvider.GetRequiredService<IAgent2AgentContextRepository>();
        var httpContextAccessor = serviceProvider.GetRequiredService<IHttpContextAccessor>();
        var userGroupIds = httpContextAccessor.HttpContext?.User.GetGroupClaims();
        var currentTask = await taskRepo.GetTaskAsync(taskId, cancellationToken);
        var context = await contextRepo.GetContextAsync(currentTask?.ContextId!, cancellationToken);

        if (context == null)
            return "Context not found".ToErrorCallToolResponse();

        var userAllowed =
            (context.UserIds != null && context.UserIds.Contains(oid)) ||
            (context.SecurityGroupIds != null && userGroupIds != null && context.SecurityGroupIds.Intersect(userGroupIds).Any());

        if (!userAllowed)
            return "You do not have access to this task's context".ToErrorCallToolResponse();

        var artifacts = currentTask?.Artifacts?.SelectMany(a => a.Parts.OfType<FilePart>());
        List<ResourceLinkBlock> resources = [];

        foreach (var artifact in artifacts?.Where(a => a.File.Bytes != null) ?? [])
        {
            var url = await requestContext.Server.Upload(serviceProvider, artifact.File.Name!,
                BinaryData.FromBytes(Convert.FromBase64String(artifact.File.Bytes!)), cancellationToken);

            if (url != null)
                resources.Add(url);
        }

        return resources.ToResourceLinkCallToolResponse();
    }

    [Description("Delete an A2A task")]
    [McpServerTool(Name = "agent2agent_delete_task", Title = "Delete an A2A task",
       OpenWorld = false)]
    public static async Task<CallToolResult> Agent2Agent_DeleteTask(
       [Description("Id of the task")] string taskId,
       RequestContext<CallToolRequestParams> requestContext,
       IServiceProvider serviceProvider,
       CancellationToken cancellationToken = default)
    {
        var taskRepo = serviceProvider.GetRequiredService<IAgent2AgentTaskRepository>();
        var oid = serviceProvider.GetUserId();
        var contextRepo = serviceProvider.GetRequiredService<IAgent2AgentContextRepository>();
        var taskItem = await taskRepo.GetTaskAsync(taskId, cancellationToken);
        var context = await contextRepo.GetContextAsync(taskItem?.ContextId!, cancellationToken);

        if (context == null)
            return "Context not found".ToErrorCallToolResponse();

        var userAllowed =
            context.OwnerIds != null && context.OwnerIds.Contains(oid);

        if (!userAllowed)
            return "You do not have access to this task's context".ToErrorCallToolResponse();

        // One-liner again
        return await requestContext.ConfirmAndDeleteAsync<DeleteA2ATask>(
            context.Metadata.TryGetValue("name", out object? value) ? value.ToString()! : context.ContextId,
            async _ => await taskRepo.DeleteTaskAsync(taskId, cancellationToken),
            "Task deleted.",
            cancellationToken);
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

    [Description("Please confirm the name of the context you want to delete: {0}")]
    public class DeleteA2AContext : IHasName
    {
        [JsonPropertyName("name")]
        [Required]
        [Description("The name of the context to delete.")]
        public string Name { get; set; } = default!;

    }

    [Description("Please confirm the name of the task you want to delete: {0}")]
    public class DeleteA2ATask : IHasName
    {
        [JsonPropertyName("name")]
        [Required]
        [Description("The name of the context to delete.")]
        public string Name { get; set; } = default!;
    }

}

