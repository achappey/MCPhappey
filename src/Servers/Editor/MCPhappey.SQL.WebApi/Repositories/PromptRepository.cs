using MCPhappey.SQL.WebApi.Context;
using MCPhappey.SQL.WebApi.Models.Database;
using Microsoft.EntityFrameworkCore;

namespace MCPhappey.SQL.WebApi.Repositories;

public class PromptRepository(McpDatabaseContext databaseContext)
{
    public async Task<List<Prompt>> GetPrompts(string serverName, CancellationToken cancellationToken = default) =>
       await databaseContext.Servers.AsNoTracking()
           .Where(a => a.Name == serverName)
           .Include(r => r.Prompts)
           .ThenInclude(r => r.Arguments)
           .Include(r => r.Prompts)
           .SelectMany(a => a.Prompts)
           .ToListAsync(cancellationToken);

    public async Task<Prompt?> GetPrompt(string serverName, string promptName, CancellationToken cancellationToken = default) =>
        await databaseContext.Servers.AsNoTracking()
        .Where(a => a.Name == serverName)
        .Include(r => r.Prompts)
        .ThenInclude(r => r.Arguments)
        .Include(r => r.Prompts)
        .SelectMany(a => a.Prompts)
        .FirstOrDefaultAsync(a => a.Name == promptName, cancellationToken);

    public async Task<Prompt> UpdatePrompt(Prompt prompt)
    {
        databaseContext.Prompts.Update(prompt);
        await databaseContext.SaveChangesAsync();

        return prompt;
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

    public async Task<Prompt> AddServerPrompt(int serverId,
        string prompt,
        string name,
        string? description = null,
        IEnumerable<PromptArgument>? arguments = null,
        IEnumerable<PromptResource>? promptResources = null)
    {
        var item = await databaseContext.Prompts.AddAsync(new()
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
