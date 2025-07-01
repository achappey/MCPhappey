using A2A.Server.Infrastructure;
using Azure.Storage.Blobs;
using MCPhappey.Agent2Agent.Providers;
using System.Text.Json;

namespace MCPhappey.Agent2Agent.Repositories;

public interface IAgent2AgentTaskRepository
{
    Task<TaskRecord?> GetTaskAsync(string taskId, CancellationToken ct = default);
    Task<IEnumerable<TaskRecord>> GetTasksByContextAsync(string contextId, CancellationToken ct = default);
    Task SaveTaskAsync(TaskRecord task, CancellationToken ct = default);
    Task DeleteTaskAsync(string taskId, CancellationToken ct = default);
}

public class Agent2AgentTaskRepository : IAgent2AgentTaskRepository
{
    private readonly BlobContainerClient _container;

    public Agent2AgentTaskRepository(ITasksBlobContainerProvider provider)
    {
        _container = provider.Client;
    }

    public async Task<TaskRecord?> GetTaskAsync(string taskId, CancellationToken ct = default)
    {
        var blob = _container.GetBlobClient($"{taskId}.json");
        if (!(await blob.ExistsAsync(ct))) return null;
        var stream = await blob.OpenReadAsync(cancellationToken: ct);
        return await JsonSerializer.DeserializeAsync<TaskRecord>(stream, cancellationToken: ct);
    }

    public async Task<IEnumerable<TaskRecord>> GetTasksByContextAsync(string contextId, CancellationToken ct = default)
    {
        var results = new List<TaskRecord>();
        await foreach (var blob in _container.GetBlobsAsync(cancellationToken: ct))
        {
            var client = _container.GetBlobClient(blob.Name);
            var stream = await client.OpenReadAsync(cancellationToken: ct);
            var task = await JsonSerializer.DeserializeAsync<TaskRecord>(stream, cancellationToken: ct);
            if (task?.ContextId == contextId)
                results.Add(task);
        }
        return results;
    }

    public async Task SaveTaskAsync(TaskRecord task, CancellationToken ct = default)
    {
        var blob = _container.GetBlobClient($"{task.Id}.json");
        using var stream = new MemoryStream(JsonSerializer.SerializeToUtf8Bytes(task));
        await blob.UploadAsync(stream, overwrite: true, cancellationToken: ct);
    }

    public async Task DeleteTaskAsync(string taskId, CancellationToken ct = default)
    {
        var blob = _container.GetBlobClient($"{taskId}.json");
        await blob.DeleteIfExistsAsync(cancellationToken: ct);
    }
}
