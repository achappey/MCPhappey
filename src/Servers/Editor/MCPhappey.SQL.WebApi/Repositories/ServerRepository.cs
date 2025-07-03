using MCPhappey.SQL.WebApi.Context;
using MCPhappey.SQL.WebApi.Models.Database;
using Microsoft.EntityFrameworkCore;

namespace MCPhappey.SQL.WebApi.Repositories;

public class ServerRepository(McpDatabaseContext databaseContext)
{
    public async Task<Server?> GetServer(string name, CancellationToken cancellationToken = default) =>
        await databaseContext.Servers
            .Include(r => r.Prompts)
            .ThenInclude(r => r.Arguments)
            .Include(r => r.Prompts)
            .ThenInclude(r => r.PromptResources)
            .ThenInclude(r => r.Resource)
            .Include(r => r.Prompts)
            .ThenInclude(r => r.PromptResourceTemplates)
            .ThenInclude(r => r.ResourceTemplate)
            .Include(r => r.Resources)
            .Include(r => r.ResourceTemplates)
            .Include(r => r.Owners)
            .Include(r => r.Tools)
            .Include(r => r.Groups)
            .FirstOrDefaultAsync(r => r.Name == name, cancellationToken);

    public async Task<List<Server>> GetServers(CancellationToken cancellationToken = default) =>
        await databaseContext.Servers.AsNoTracking()
            .Include(r => r.Prompts)
            .ThenInclude(r => r.Arguments)
            .Include(r => r.Prompts)
            .ThenInclude(r => r.PromptResources)
            .ThenInclude(r => r.Resource)
            .Include(r => r.Prompts)
            .ThenInclude(r => r.PromptResourceTemplates)
            .ThenInclude(r => r.ResourceTemplate)
            .Include(r => r.Resources)
            .Include(r => r.Owners)
            .Include(r => r.Tools)
            .Include(r => r.Groups)
            .Include(r => r.ResourceTemplates)
            .ToListAsync(cancellationToken);

    public async Task<Server> UpdateServer(Server server)
    {
        databaseContext.Servers.Update(server);
        await databaseContext.SaveChangesAsync();

        return server;
    }

    public async Task<Server> CreateServer(Server server, CancellationToken cancellationToken)
    {
        await databaseContext.Servers.AddAsync(server, cancellationToken);
        await databaseContext.SaveChangesAsync(cancellationToken);

        return server;
    }

    public async Task DeleteServer(int id)
    {
        var item = await databaseContext.Servers.FirstOrDefaultAsync(a => a.Id == id);

        if (item != null)
        {
            databaseContext.Servers.Remove(item);
            await databaseContext.SaveChangesAsync();
        }
    }

    public async Task DeleteTool(int serverId, string name)
    {
        var item = await databaseContext.Tools.FirstOrDefaultAsync(a => a.ServerId == serverId && a.Name == name);

        if (item != null)
        {
            databaseContext.Tools.Remove(item);
            await databaseContext.SaveChangesAsync();
        }
    }

    public async Task AddServerOwner(int serverId, string owner)
    {

        await databaseContext.ServerOwners.AddAsync(new()
        {
            Id = owner,
            ServerId = serverId
        });

        await databaseContext.SaveChangesAsync();
    }

    public async Task AddServerTool(int serverId, string toolName)
    {
        await databaseContext.Tools.AddAsync(new ServerTool()
        {
            Name = toolName,
            ServerId = serverId
        });

        await databaseContext.SaveChangesAsync();
    }
}
