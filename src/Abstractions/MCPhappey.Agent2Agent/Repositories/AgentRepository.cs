using MCPhappey.Agent2Agent.Database.Models;
using MCPhappey.Servers.SQL.Context;
using Microsoft.EntityFrameworkCore;

namespace MCPhappey.Agent2Agent.Repositories;

public class AgentRepository(A2ADatabaseContext databaseContext)
{
    public async Task<bool?> AgentExists(string name, string url, CancellationToken cancellationToken = default) =>
       await databaseContext.AgentCards
           .AnyAsync(a => a.Name == name || a.Url == url, cancellationToken);

    public async Task<Agent?> GetAgent(string name, CancellationToken cancellationToken = default) =>
        await databaseContext.Agents
            .Include(r => r.Owners)
            .Include(r => r.AgentCard)
            .ThenInclude(r => r.Skills)
            .ThenInclude(r => r.SkillTags)
            .ThenInclude(r => r.Tag)
            .Include(r => r.AgentCard)
            .ThenInclude(r => r.Skills)
            .ThenInclude(r => r.Examples)
            .FirstOrDefaultAsync(r => r.AgentCard.Name == name, cancellationToken);

    public async Task<McpServer?> GetMcpServer(string url, CancellationToken cancellationToken = default) =>
        await databaseContext.McpServers
        .FirstOrDefaultAsync(r => r.Url == url, cancellationToken);

    public async Task<List<Agent>> GetAgents(CancellationToken cancellationToken = default) =>
        await databaseContext.Agents.AsNoTracking()
            .Include(r => r.Owners)
            .Include(r => r.AgentCard)
            .ThenInclude(r => r.Skills)
            .ThenInclude(r => r.SkillTags)
            .ThenInclude(r => r.Tag)
            .Include(r => r.AgentCard)
            .ThenInclude(r => r.Skills)
            .ThenInclude(r => r.Examples)
            .ToListAsync(cancellationToken);

    public async Task<Agent> UpdateAgent(Agent agent)
    {
        databaseContext.Agents.Update(agent);
        await databaseContext.SaveChangesAsync();

        return agent;
    }
    
    public async Task<Skill> UpdateSkill(Skill agent)
    {
        databaseContext.Skills.Update(agent);
        await databaseContext.SaveChangesAsync();

        return agent;
    }

    public async Task<McpServer> CreateMcpServer(McpServer server, CancellationToken cancellationToken)
    {
        var result = await databaseContext.McpServers.AddAsync(server, cancellationToken);
        await databaseContext.SaveChangesAsync(cancellationToken);

        return result.Entity;
    }

    public async Task<Agent> CreateAgent(Agent agent, CancellationToken cancellationToken)
    {
        var result = await databaseContext.Agents.AddAsync(agent, cancellationToken);
        await databaseContext.SaveChangesAsync(cancellationToken);

        return result.Entity;
    }


    public async Task<Skill> CreateSkill(Skill skill, CancellationToken cancellationToken)
    {
        var result = await databaseContext.Skills.AddAsync(skill, cancellationToken);
        await databaseContext.SaveChangesAsync(cancellationToken);

        return result.Entity;
    }


    public async Task<Tag> CreateTag(Tag skill, CancellationToken cancellationToken)
    {
        var result = await databaseContext.Tags.AddAsync(skill, cancellationToken);
        await databaseContext.SaveChangesAsync(cancellationToken);

        return result.Entity;
    }

    public async Task<AppRegistration> CreateAppRegistration(AppRegistration skill, CancellationToken cancellationToken)
    {
        var result = await databaseContext.AppRegistrations.AddAsync(skill, cancellationToken);
        await databaseContext.SaveChangesAsync(cancellationToken);

        return result.Entity;
    }
    public async Task DeleteAgent(int id)
    {
        var item = await databaseContext.Agents.FirstOrDefaultAsync(a => a.Id == id);

        if (item != null)
        {
            databaseContext.Agents.Remove(item);
            await databaseContext.SaveChangesAsync();
        }
    }
}
