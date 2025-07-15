using MCPhappey.Common.Models;
using MCPhappey.Core.Services;
using MCPhappey.Servers.SQL.Extensions;
using MCPhappey.Servers.SQL.Repositories;
using ModelContextProtocol.Protocol;

namespace MCPhappey.Servers.SQL.Providers;

public class SqlServerDataProvider(ServerRepository serverRepository) : IServerDataProvider
{
    public async Task<IEnumerable<PromptTemplate>> GetPromptsAsync(string serverName, CancellationToken ct = default)
    {
        var server = await serverRepository.GetServer(serverName, ct);

        return server?.Prompts.ToPromptTemplates().Prompts ?? [];
    }

    public async Task<ListResourcesResult> GetResourcesAsync(string serverName, CancellationToken ct = default)
    {
        var server = await serverRepository.GetServer(serverName, ct);

        return server?.Resources.ToListResourcesResult() ?? new ListResourcesResult();
    }

    public async Task<ListResourceTemplatesResult> GetResourceTemplatesAsync(string serverName, CancellationToken ct = default)
    {
        var server = await serverRepository.GetServer(serverName, ct);

        return server?.ResourceTemplates.ToListResourceTemplatesResult() ?? new ListResourceTemplatesResult();
    }
}
