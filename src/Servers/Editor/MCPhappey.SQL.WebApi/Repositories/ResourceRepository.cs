using MCPhappey.SQL.WebApi.Context;
using MCPhappey.SQL.WebApi.Models.Database;
using Microsoft.EntityFrameworkCore;

namespace MCPhappey.SQL.WebApi.Repositories;

public class ResourceRepository(McpDatabaseContext databaseContext)
{
    public async Task<List<Resource>> GetResources(string serverName, CancellationToken cancellationToken = default) =>
     await databaseContext.Servers.AsNoTracking()
         .Where(a => a.Name == serverName)
         .Include(r => r.Resources)
         .SelectMany(a => a.Resources)
         .ToListAsync(cancellationToken);

    public async Task<Resource?> GetResource(string serverName, string resourceUri, CancellationToken cancellationToken = default) =>
        await databaseContext.Servers.AsNoTracking()
        .Where(a => a.Name == serverName)
        .Include(r => r.Resources)
        .SelectMany(a => a.Resources)
        .FirstOrDefaultAsync(a => a.Uri == resourceUri, cancellationToken);


    public async Task<ResourceTemplate?> GetResourceTemplate(int id) =>
        await databaseContext.ResourceTemplates
                .FirstOrDefaultAsync(r => r.Id == id);

    public async Task<Resource?> GetResource(int id) =>
        await databaseContext.Resources
            .FirstOrDefaultAsync(r => r.Id == id);


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

    public async Task DeleteResource(int id)
    {
        var item = await databaseContext.Resources.FirstOrDefaultAsync(a => a.Id == id);

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

}
