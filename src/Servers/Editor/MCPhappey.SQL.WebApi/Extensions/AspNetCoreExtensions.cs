using MCPhappey.Common.Models;
using MCPhappey.SQL.WebApi.Context;
using MCPhappey.SQL.WebApi.Repositories;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MCPhappey.SQL.WebApi.Extensions;

public static class AspNetCoreExtensions
{
    public static IEnumerable<ServerConfig> AddSqlMcpServers(
        this WebApplicationBuilder builder,
        string mcpDatabase)
    {
        builder.Services.AddDbContext<McpDatabaseContext>(options =>
            options.UseSqlServer(mcpDatabase, sqlOpts => sqlOpts.EnableRetryOnFailure()));
        builder.Services.AddScoped<ServerRepository>();
        using var tempProvider = builder.Services.BuildServiceProvider();
        using var scope = tempProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<McpDatabaseContext>();

        return db.Servers
                .Include(a => a.Resources)
                .Include(a => a.ResourceTemplates)
                .Include(a => a.Prompts)
                .ThenInclude(a => a.Arguments)
                .Include(a => a.Prompts)
                .ThenInclude(a => a.PromptResources)
                .ThenInclude(a => a.Resource)
                .Include(a => a.Prompts)
                .ThenInclude(a => a.PromptResourceTemplates)
                .ThenInclude(a => a.ResourceTemplate)
                .Include(a => a.Tools)
                .Include(a => a.Owners)
                .Include(a => a.Groups)
                .AsNoTracking()
                .ToList()
                .Select(a => new ServerConfig()
                {
                    Server = a.ToMcpServer(),
                    ResourceList = a.Resources.ToListResourcesResult(),
                    ResourceTemplateList = a.ResourceTemplates.ToListResourceTemplatesResult(),
                    PromptList = a.Prompts.ToPromptTemplates()
                });
    }
}