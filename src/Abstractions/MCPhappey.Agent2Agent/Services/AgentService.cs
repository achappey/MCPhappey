using MCPhappey.Agent2Agent.Extensions;
using MCPhappey.Agent2Agent.Models;
using MCPhappey.Agent2Agent.Repositories;

namespace MCPhappey.Agent2Agent.Services;

public class AgentService(
    AgentRepository agentRepository)
{

    public async Task<IEnumerable<AgentConfig>> GetAgents(string userId, CancellationToken cancellationToken = default)
    {
        var items = await agentRepository.GetAgents(cancellationToken);

        return items.Where(r => r.Owners.Any(r => r.Id == userId))
            .Select(t => t.ToAgentConfig());
    }

    public async Task<AgentConfig?> GetAgent(string name, string userId, CancellationToken cancellationToken = default)
    {
        var item = await agentRepository.GetAgent(name, cancellationToken);

        if (item?.Owners.Any(r => r.Id == userId) == false)
            throw new UnauthorizedAccessException();

        return item?.ToAgentConfig();
    }

}
