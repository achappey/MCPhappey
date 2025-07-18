using System.Text.Json;
using Azure.Storage.Blobs;
using MCPhappey.Agent2Agent.Providers;

namespace MCPhappey.Agent2Agent.Repositories;

public interface IAgent2AgentContextRepository
{
    Task<Agent2AgentOrchestrator.Agent2AgentContext?> GetContextAsync(string contextId, CancellationToken ct = default);
    Task<IEnumerable<Agent2AgentOrchestrator.Agent2AgentContext>> GetAllContextsAsync(CancellationToken ct = default);
    Task<IEnumerable<Agent2AgentOrchestrator.Agent2AgentContext>> GetContextsForUserAsync(string userId, CancellationToken ct = default);
    Task SaveContextAsync(Agent2AgentOrchestrator.Agent2AgentContext ctx, CancellationToken ct = default);
    Task DeleteContextAsync(string contextId, CancellationToken ct = default);
}

public class Agent2AgentContextRepository : IAgent2AgentContextRepository
{
    private readonly BlobContainerClient _container;

    public Agent2AgentContextRepository(IContextsBlobContainerProvider provider)
    {
        _container = provider.Client;
    }

    public async Task<Agent2AgentOrchestrator.Agent2AgentContext?> GetContextAsync(string contextId, CancellationToken ct = default)
    {
        var blob = _container.GetBlobClient($"{contextId}.json");
        if (!(await blob.ExistsAsync(ct))) return null;
        var stream = await blob.OpenReadAsync(cancellationToken: ct);
        return await JsonSerializer.DeserializeAsync<Agent2AgentOrchestrator.Agent2AgentContext>(stream, cancellationToken: ct);
    }

    public async Task<IEnumerable<Agent2AgentOrchestrator.Agent2AgentContext>> GetAllContextsAsync(CancellationToken ct = default)
    {
        var results = new List<Agent2AgentOrchestrator.Agent2AgentContext>();
        await foreach (var blob in _container.GetBlobsAsync(cancellationToken: ct))
        {
            var client = _container.GetBlobClient(blob.Name);
            var stream = await client.OpenReadAsync(cancellationToken: ct);
            var ctx = await JsonSerializer.DeserializeAsync<Agent2AgentOrchestrator.Agent2AgentContext>(stream, cancellationToken: ct);
            if (ctx != null)
                results.Add(ctx);
        }
        return results;
    }

    public async Task<IEnumerable<Agent2AgentOrchestrator.Agent2AgentContext>> GetContextsForUserAsync(string userId, CancellationToken ct = default)
    {
        var all = await GetAllContextsAsync(ct);
        return all.Where(c => c.UserIds.Contains(userId));
    }

    public async Task SaveContextAsync(Agent2AgentOrchestrator.Agent2AgentContext ctx, CancellationToken ct = default)
    {
        var blob = _container.GetBlobClient($"{ctx.ContextId}.json");
        using var stream = new MemoryStream(JsonSerializer.SerializeToUtf8Bytes(ctx));
        await blob.UploadAsync(stream, overwrite: true, cancellationToken: ct);
    }

    public async Task DeleteContextAsync(string contextId, CancellationToken ct = default)
    {
        var blob = _container.GetBlobClient($"{contextId}.json");
        await blob.DeleteIfExistsAsync(cancellationToken: ct);
    }
}
