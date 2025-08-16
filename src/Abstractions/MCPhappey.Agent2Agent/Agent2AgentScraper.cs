using MCPhappey.Common;
using MCPhappey.Common.Models;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Server;
using MCPhappey.Common.Extensions;
using MCPhappey.Auth.Extensions;
using System.Collections.Concurrent;
using MCPhappey.Agent2Agent.Repositories;
using MCPhappey.Core.Extensions;
using MCPhappey.Agent2Agent.Services;
using MCPhappey.Agent2Agent.Extensions;
using Microsoft.AspNetCore.Http;
using A2A.Server.Infrastructure;

namespace MCPhappey.Agent2Agent;

public class Agent2AgentScraper(
    IHttpClientFactory httpClientFactory) : IContentScraper
{
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;

    private readonly ConcurrentDictionary<string, (string Key, string Secret)> _secretsCache = new();

    public bool SupportsHost(ServerConfig currentConfig, string host)
        => new Uri(host).Scheme == "a2a";

    public async Task<IEnumerable<FileItem>?> GetContentAsync(
        IMcpServer mcpServer,
        IServiceProvider serviceProvider,
        string url,
        CancellationToken cancellationToken = default)
    {
        var tokenProvider = serviceProvider.GetRequiredService<HeaderProvider>();
        var oid = tokenProvider.GetOidClaim();
        ArgumentException.ThrowIfNullOrWhiteSpace(oid);

        var contextRepo = serviceProvider.GetRequiredService<IAgent2AgentContextRepository>();
        var taskRepo = serviceProvider.GetRequiredService<IAgent2AgentTaskRepository>();
        var contextService = serviceProvider.GetRequiredService<ContextService>();
        var agentService = serviceProvider.GetRequiredService<AgentService>();
        using var graphClient = await serviceProvider.GetOboGraphClient(mcpServer);

        var uri = new Uri(url);
        var host = uri.Host;
        var segments = uri.AbsolutePath.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        var httpContextAccessor = serviceProvider.GetRequiredService<IHttpContextAccessor>();
        var name = tokenProvider.GetNameClaim();
        var userGroupIds = httpContextAccessor.HttpContext?.User.GetGroupClaims();

        // a2a://context or a2a://context/
        if ((host.Equals("context", StringComparison.OrdinalIgnoreCase) && segments.Length == 0) ||
            (segments.Length == 1 && segments[0].Equals("context", StringComparison.OrdinalIgnoreCase)))
        {
            var contexts = await contextService.GetUserContextsWithUsersAsync(graphClient, oid, userGroupIds ?? [], cancellationToken);

            return [contexts.ToFileItem(url)];
        }

        if ((host.Equals("agents", StringComparison.OrdinalIgnoreCase) && segments.Length == 0) ||
          (segments.Length == 1 && segments[0].Equals("agents", StringComparison.OrdinalIgnoreCase)))
        {
            var contexts = await agentService.GetAgents(oid, cancellationToken);

            return [contexts.ToFileItem(url)];
        }

        // a2a://context/{contextId}
        if (host.Equals("context", StringComparison.OrdinalIgnoreCase) && segments.Length == 1)
        {
            var contextId = segments[0];
            var hasAccess = await contextRepo.HasContextAccess(contextId, oid, cancellationToken);
            if (!hasAccess) throw new UnauthorizedAccessException();
            var context = await contextService.GetContextAsync(graphClient, contextId, oid, cancellationToken);
            if (context == null) throw new UnauthorizedAccessException();

            return [context.ToFileItem(url)];
        }

        // a2a://context/{contextId}/tasks
        if (host.Equals("context", StringComparison.OrdinalIgnoreCase) &&
            segments.Length == 2 &&
            segments[1].Equals("tasks", StringComparison.OrdinalIgnoreCase))
        {
            var contextId = segments[0];

            var hasAccess = await contextRepo.HasContextAccess(contextId, oid, cancellationToken);
            if (!hasAccess) throw new UnauthorizedAccessException();
            var context = await contextRepo.GetContextAsync(contextId, cancellationToken);
            if (context == null) throw new UnauthorizedAccessException();

            var tasks = await taskRepo.GetTasksByContextAsync(contextId, cancellationToken);

            return [.. tasks.Select(a => a.ToTaskFileItem($"a2a://task/{a.Id}"))];
        }

        // a2a://tasks
        if ((host.Equals("tasks", StringComparison.OrdinalIgnoreCase) && segments.Length == 0) ||
            (segments.Length == 1 && segments[0].Equals("tasks", StringComparison.OrdinalIgnoreCase)))
        {
            var contexts = await contextService.GetUserContextsWithUsersAsync(graphClient, oid, userGroupIds ?? [], cancellationToken);

            List<TaskRecord> allTasks = [];
            foreach (var context in contexts)
            {
                var tasks = await taskRepo.GetTasksByContextAsync(context.ContextId, cancellationToken);

                allTasks.AddRange(tasks);
            }

            return [.. allTasks.Select(a => a.ToTaskFileItem($"a2a://task/{a.Id}"))];
        }

        // a2a://task/{taskId}
        if (host.Equals("task", StringComparison.OrdinalIgnoreCase) && segments.Length == 1)
        {
            var taskId = segments[0];
            var task = await taskRepo.GetTaskAsync(taskId, cancellationToken);
            var hasAccess = await contextRepo.HasContextAccess(task?.ContextId!, oid, cancellationToken);
            if (!hasAccess) throw new UnauthorizedAccessException();

            return task != null ? [task.ToTaskFileItem(url, $"{taskId}.json")] : [];
        }

        // a2a://task/{taskId}/artifact
        if (host.Equals("task", StringComparison.OrdinalIgnoreCase) &&
            segments.Length == 2 &&
            segments[1].Equals("artifact", StringComparison.OrdinalIgnoreCase))
        {
            var taskId = segments[0];
            var task = await taskRepo.GetTaskAsync(taskId, cancellationToken);
            var hasAccess = await contextRepo.HasContextAccess(task?.ContextId!, oid, cancellationToken);
            if (!hasAccess) throw new UnauthorizedAccessException();

            return task?.Artifacts != null
                ? [.. task.Artifacts.Select(a => a.ToArtifactFileItem($"a2a://task/{taskId}/artifact/{a.ArtifactId}"))] // or .ToArray(), depending on what ToFileItem accepts
                                                                                                                        // .ToFileItem(url)
                : new List<FileItem>(); // or Array.Empty<FileItem>()
        }

        // a2a://task/{taskId}/artifact/{artifactId}
        if (host.Equals("task", StringComparison.OrdinalIgnoreCase) &&
            segments.Length == 3 &&
            segments[1].Equals("artifact", StringComparison.OrdinalIgnoreCase))
        {
            var taskId = segments[0];
            var artifactId = segments[2];

            var task = await taskRepo.GetTaskAsync(taskId, cancellationToken);
            var hasAccess = await contextRepo.HasContextAccess(task?.ContextId!, oid, cancellationToken);
            if (!hasAccess) throw new UnauthorizedAccessException();

            var artifact = task?.Artifacts?.FirstOrDefault(t => t.ArtifactId == artifactId);

            return artifact != null ? [artifact.ToFileItem(url)] : [];
        }

        throw new NotSupportedException();
    }

}
