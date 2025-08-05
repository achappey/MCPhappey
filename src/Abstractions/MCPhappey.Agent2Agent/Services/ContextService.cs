using MCPhappey.Agent2Agent.Models;
using MCPhappey.Agent2Agent.Repositories;
using Microsoft.Graph.Beta;
using ModelContextProtocol.Server;

namespace MCPhappey.Agent2Agent.Services;

public class ContextService
{
    private readonly IAgent2AgentContextRepository _contextRepo;

    public ContextService(
        IAgent2AgentContextRepository contextRepo)
    {
        _contextRepo = contextRepo;
    }
    public async Task<List<Agent2AgentViewContext>> GetUserContextsWithUsersAsync(GraphServiceClient graphServiceClient,
        string userId, CancellationToken cancellationToken = default)
    {
        var contexts = await _contextRepo.GetContextsForUserAsync(userId, cancellationToken);

        var userIds = contexts.SelectMany(c => c.OwnerIds)
            .Concat(contexts.SelectMany(c => c.UserIds))
            .Distinct()
            .ToList();

        var usersResponse = userIds.Any() ? await graphServiceClient.Users.GetByIds
            .PostAsGetByIdsPostResponseAsync(new Microsoft.Graph.Beta.Users.GetByIds.GetByIdsPostRequestBody
            {
                Ids = userIds
            }, cancellationToken: cancellationToken) : null;

        var usersDict = usersResponse?.Value?
            .Where(u => u != null && !string.IsNullOrEmpty(u.Id))
            .ToDictionary(u => u.Id!, u => u) ?? [];

        return contexts.Select(ctx => ctx.ToViewContext(usersDict)).ToList();
    }

    public async Task<Agent2AgentViewContext?> GetContextAsync(GraphServiceClient graphServiceClient,
        string contextId, string userId, CancellationToken cancellationToken = default)
    {
        var hasAccess = await _contextRepo.HasContextAccess(contextId, userId, cancellationToken);
        if (!hasAccess) return null;

        var ctx = await _contextRepo.GetContextAsync(contextId, cancellationToken);
        if (ctx == null) return null;

        var userIds = ctx.OwnerIds.Concat(ctx.UserIds).Distinct().ToList();

        var usersResponse = await graphServiceClient.Users.GetByIds
            .PostAsGetByIdsPostResponseAsync(new Microsoft.Graph.Beta.Users.GetByIds.GetByIdsPostRequestBody
            {
                Ids = userIds
            }, cancellationToken: cancellationToken);

        var usersDict = usersResponse?.Value?
            .Where(u => u != null && !string.IsNullOrEmpty(u.Id))
            .ToDictionary(u => u.Id!, u => u) ?? [];

        return ctx.ToViewContext(usersDict);
    }


}
