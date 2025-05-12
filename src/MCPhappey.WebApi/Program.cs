using MCPhappey.Core.Extensions;
using MCPhappey.WebApi;
using MCPhappey.Servers.JSON;
using Microsoft.KernelMemory;
using MCPhappey.Servers.SQL.Extensions;
using MCPhappey.Decoders.Extensions;
using MCPhappey.Common.Constants;
using MCPhappey.Auth.Models;
using MCPhappey.Auth.Extensions;
using MCPhappey.Core.Services;

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

var oauthSettings = builder.Configuration
    .GetSection("OAuth")
    .Get<OAuthSettings>();

if (oauthSettings != null)
{
    builder.Services.AddSingleton(oauthSettings);
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

builder.AddAuthServices(appConfig?.PrivateKey ?? "", appConfig?.Domains);
builder.AddMcpCoreServices(servers, appConfig?.Domains);

var app = builder.Build();
app.UseCors("AllowSpecificOrigin");
app.UseRouting();

if (oauthSettings != null)
{
    app.MapOAuth([.. servers.Where(a => a.Server.HasAuth())], oauthSettings);
}

app.UseMcpWebApplication(servers);

app.Run();
