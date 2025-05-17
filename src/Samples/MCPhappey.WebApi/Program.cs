using MCPhappey.Core.Extensions;
using MCPhappey.WebApi;
using MCPhappey.Servers.JSON;
using Microsoft.KernelMemory;
using MCPhappey.Servers.SQL.Extensions;
using MCPhappey.Decoders.Extensions;
using MCPhappey.Common.Constants;
using MCPhappey.Auth.Extensions;
using MCPhappey.Scrapers.Extensions;
using MCPhappey.Core.Services;
using MCPhappey.Simplicate.Extensions;

var builder = WebApplication.CreateBuilder(args);
var appConfig = builder.Configuration.Get<Config>();

var basePath = Path.Combine(AppContext.BaseDirectory, "Servers");
var servers = basePath.GetServers().ToList();

if (!string.IsNullOrEmpty(appConfig?.McpDatabase))
{
    servers.AddRange(builder.AddSqlMcpServers(appConfig.McpDatabase));
}

if (!string.IsNullOrEmpty(appConfig?.KernelMemoryDatabase)
    && appConfig?.Domains?.ContainsKey(Hosts.OpenAI) == true)
{
    builder.Services.AddKernelMemoryWithOptions(memoryBuilder =>
    {
        memoryBuilder
            .WithCustomWebScraper<DownloadService>()
            .WithSimpleQueuesPipeline()
            .WithOpenAI(new OpenAIConfig()
            {
                APIKey = appConfig?.Domains?
                    .FirstOrDefault(a => a.Key == Hosts.OpenAI)
                    .Value
                    .FirstOrDefault(a => a.Key == "Authorization").Value.GetBearerToken()!,
                TextModel = "gpt-4.1-2025-04-14",
                TextModelMaxTokenTotal = 65536,
                EmbeddingDimensions = 3072,
                EmbeddingModel = "text-embedding-3-large"
            })
            .WithDecoders()
            .WithSqlServerMemoryDb(new()
            {
                ConnectionString = appConfig?.KernelMemoryDatabase!
            })
            .WithSearchClientConfig(new()
            {
                MaxMatchesCount = int.MaxValue
            });
    }, new()
    {
        AllowMixingVolatileAndPersistentData = true
    });
}

if (appConfig?.OAuth != null)
{
    builder.Services.AddSingleton(appConfig.OAuth);
}

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigin", policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

if (!string.IsNullOrEmpty(appConfig?.PrivateKey))
{
    builder.AddAuthServices(appConfig.PrivateKey);
}

if (appConfig?.Domains != null)
{
    builder.Services.WithHostScrapers(appConfig.Domains);
}

if (appConfig?.OAuth != null)
{
    builder.Services.WithOboScrapers(servers, appConfig.OAuth);
}

builder.Services.WithDefaultScrapers();

if (appConfig?.Simplicate != null)
{
    builder.WithSimplicateScraper(appConfig.Simplicate, appConfig.OAuth);
    servers.ApplySimplicateOrganization(appConfig.Simplicate.Organization);
}

builder.Services.AddMcpCoreServices(servers);

var app = builder.Build();
app.UseCors("AllowSpecificOrigin");
app.UseRouting();

if (appConfig?.OAuth != null)
{
    app.MapOAuth([.. servers.Where(a => a.Server.HasAuth())], appConfig.OAuth);
}

app.UseMcpWebApplication(servers);

app.Run();
