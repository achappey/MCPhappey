using MCPhappey.Common.Models;
using MCPhappey.Servers.SQL.Context;
using MCPhappey.Servers.SQL.Repositories;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MCPhappey.Servers.SQL.Extensions;

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
                .Include(a => a.Tools)
                .AsNoTracking()
                .ToList()
                .Select(a => new ServerConfig()
                {
                    Server = a.ToMcpServer(),
                    ResourceList = a.Resources.ToListResourcesResult()
                });
    }
}