using MCPhappey.Core.Extensions;
using MCPhappey.WebApi;
using MCPhappey.Servers.JSON;
using Microsoft.KernelMemory;
using MCPhappey.Core.Services;
using MCPhappey.Servers.SQL.Extensions;

var builder = WebApplication.CreateBuilder(args);
var appConfig = builder.Configuration.Get<Config>();

var basePath = Path.Combine(AppContext.BaseDirectory, "Servers");
var servers = basePath.GetServers(appConfig?.Auth ?? []).ToList();

if (!string.IsNullOrEmpty(appConfig?.McpDatabase))
{
    servers.AddRange(builder.AddSqlMcpServers(appConfig.McpDatabase));
}

if (!string.IsNullOrEmpty(appConfig?.KernelMemoryDatabase)
    && appConfig?.Domains?.ContainsKey(MCPhappey.Core.Constants.Hosts.OpenAI) == true)
{
    builder.Services.AddKernelMemoryWithOptions(memoryBuilder =>
    {
        memoryBuilder
            .WithCustomWebScraper<DownloadService>()
            .WithSimpleQueuesPipeline()
            .WithOpenAI(new OpenAIConfig()
            {
                APIKey = appConfig?.Domains?
                    .FirstOrDefault(a => a.Key == MCPhappey.Core.Constants.Hosts.OpenAI)
                    .Value
                    .FirstOrDefault(a => a.Key == "Authorization").Value.GetBearerToken()!,
                TextModel = "gpt-4.1-2025-04-14",
                TextModelMaxTokenTotal = 65536,
                EmbeddingDimensions = 3072,
                EmbeddingModel = "text-embedding-3-large"
            })
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

builder.AddMcpCoreServices(servers, appConfig?.Domains);

var app = builder.Build();

app.UseRouting();
app.UseAuthorization();
app.UseMcpWebApplication(servers);

app.Run();
