using MCPhappey.Servers.SQL.Context;
using MCPhappey.Servers.SQL.Models;
using Microsoft.EntityFrameworkCore;

namespace MCPhappey.Servers.SQL.Repositories;

public class ServerRepository(McpDatabaseContext databaseContext)
{
    public async Task<ResourceTemplate?> GetResourceTemplate(int id) =>
        await databaseContext.ResourceTemplates
                .FirstOrDefaultAsync(r => r.Id == id);

    public async Task<Resource?> GetResource(int id) =>
        await databaseContext.Resources
            .FirstOrDefaultAsync(r => r.Id == id);

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

    public async Task<ResourceTemplate> UpdateResourceTemplate(ResourceTemplate resource)
    {
        databaseContext.ResourceTemplates.Update(resource);
        await databaseContext.SaveChangesAsync();

        return resource;
    }

    public async Task<Resource> UpdateResource(Resource resource)
    {
        databaseContext.Resources.Update(resource);
        await databaseContext.SaveChangesAsync();

        return resource;
    }

    public async Task<Prompt> UpdatePrompt(Prompt prompt)
    {
        databaseContext.Prompts.Update(prompt);
        await databaseContext.SaveChangesAsync();

        return prompt;
    }

    public async Task<PromptArgument> UpdatePromptArgument(PromptArgument promptArgument)
    {
        databaseContext.PromptArguments.Update(promptArgument);
        await databaseContext.SaveChangesAsync();

        return promptArgument;
    }

    public async Task<Server> CreateServer(Server server, CancellationToken cancellationToken)
    {
        await databaseContext.Servers.AddAsync(server, cancellationToken);
        await databaseContext.SaveChangesAsync(cancellationToken);

        return server;
    }

    public async Task DeletePromptArgument(int id)
    {
        var item = await databaseContext.PromptArguments.FirstOrDefaultAsync(a => a.Id == id);

        if (item != null)
        {
            databaseContext.PromptArguments.Remove(item);
            await databaseContext.SaveChangesAsync();
        }
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

    public async Task DeletePrompt(int id)
    {
        var item = await databaseContext.Prompts.FirstOrDefaultAsync(a => a.Id == id);

        if (item != null)
        {
            databaseContext.Prompts.Remove(item);
            await databaseContext.SaveChangesAsync();
        }
    }

    public async Task DeleteResource(int id)
    {
        var item = await databaseContext.Resources.FirstOrDefaultAsync(a => a.Id == id);

        if (item != null)
        {
            databaseContext.Resources.Remove(item);
            await databaseContext.SaveChangesAsync();

        }
    }

    public async Task DeleteResource(string uri)
    {
        var item = await databaseContext.Resources.FirstOrDefaultAsync(a => a.Uri == uri);

        if (item != null)
        {
            databaseContext.Resources.Remove(item);
            await databaseContext.SaveChangesAsync();

        }
    }

    public async Task DeleteResourceTemplate(int id)
    {
        var item = await databaseContext.ResourceTemplates.FirstOrDefaultAsync(a => a.Id == id);

        if (item != null)
        {
            databaseContext.ResourceTemplates.Remove(item);
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

    public async Task<Resource> AddServerResource(int serverId, string uri,
        string? name = null, string? description = null)
    {
        var item = await databaseContext.Resources.AddAsync(new()
        {
            Uri = uri,
            Name = name ?? string.Empty,
            Description = description,
            ServerId = serverId
        });

        await databaseContext.SaveChangesAsync();

        return item.Entity;
    }


    public async Task<ResourceTemplate> AddServerResourceTemplate(int serverId,
        string uri,
        string? name = null,
        string? description = null)
    {
        var item = await databaseContext.ResourceTemplates.AddAsync(new()
        {
            TemplateUri = uri,
            Name = name ?? string.Empty,
            Description = description,
            ServerId = serverId
        });

        await databaseContext.SaveChangesAsync();

        return item.Entity;
    }

    public async Task<Prompt> AddServerPrompt(int serverId,
        string prompt,
        string name,
        string? description = null,
        IEnumerable<PromptArgument>? arguments = null,
        IEnumerable<PromptResource>? promptResources = null)
    {
        var item = await databaseContext.Prompts.AddAsync(new Prompt()
        {
            PromptTemplate = prompt,
            Name = name ?? string.Empty,
            Description = description,
            Arguments = arguments?.ToList() ?? [],
            ServerId = serverId,
            PromptResources = promptResources?.ToList() ?? [],
        });

        await databaseContext.SaveChangesAsync();

        return item.Entity;
    }
}
