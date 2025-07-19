using MCPhappey.Common;
using MCPhappey.Common.Models;
using MCPhappey.Core.Services;
using MCPhappey.Servers.SQL.Context;
using MCPhappey.Servers.SQL.Providers;
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
        builder.Services.AddScoped<IServerDataProvider, SqlServerDataProvider>();
        builder.Services.AddSingleton<IAutoCompletion, EditorCompletion>();
        builder.Services.AddSingleton<IAutoCompletion, DefaultCompletion>();
        builder.Services.AddSingleton<IContentScraper, McpEditorScraper>();

        using var tempProvider = builder.Services.BuildServiceProvider();
        using var scope = tempProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<McpDatabaseContext>();

        return db.Servers
                .Include(a => a.Resources)
                .Include(a => a.ResourceTemplates)
                .Include(a => a.Prompts)
                .ThenInclude(a => a.Arguments)
                .Include(a => a.Tools)
                .Include(a => a.Owners)
                .Include(a => a.Groups)
                .AsNoTracking()
                .ToList()
                .Select(a => new ServerConfig()
                {
                    Server = a.ToMcpServer(),
                    SourceType = ServerSourceType.Dynamic,
                });
    }
}