using Azure.Storage.Blobs;
using MCPhappey.Agent2Agent.Providers;
using MCPhappey.Agent2Agent.Repositories;
using MCPhappey.Agent2Agent.Services;
using MCPhappey.Common;
using MCPhappey.Servers.SQL.Context;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MCPhappey.Agent2Agent.Extensions;

//
// Summary:
//     ASP-NET Core helper to register Simplicate content-scraper plus the
//     required Azure Key Vault SecretClient.
//
public static class AspNetCoreExtensions
{

    public static WebApplicationBuilder WithAgent2Agent(
        this WebApplicationBuilder builder,
        string blobConnectionString,
        string taskContainer,
        string contextContainer,
        string database)
    {
         builder.Services.AddDbContext<A2ADatabaseContext>(options =>
            options.UseSqlServer(database, sqlOpts => sqlOpts.EnableRetryOnFailure()));

        builder.Services.AddSingleton(_ => new BlobServiceClient(blobConnectionString));
        builder.Services.AddSingleton<IContextsBlobContainerProvider, ContextsBlobContainerProvider>((sp) =>
                 new ContextsBlobContainerProvider(sp.GetRequiredService<BlobServiceClient>(), contextContainer));
        builder.Services.AddSingleton<ITasksBlobContainerProvider, TasksBlobContainerProvider>((sp) =>
                   new TasksBlobContainerProvider(sp.GetRequiredService<BlobServiceClient>(), taskContainer));

        builder.Services.AddSingleton<IAgent2AgentTaskRepository, Agent2AgentTaskRepository>();
        builder.Services.AddSingleton<IAgent2AgentContextRepository, Agent2AgentContextRepository>();
        builder.Services.AddSingleton<IContentScraper, Agent2AgentScraper>();
        builder.Services.AddSingleton<ContextService>();
        builder.Services.AddScoped<AgentRepository>();
        
        return builder;
    }
}
