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
using OpenAI;
using MCPhappey.Agent2Agent.Extensions;
using Microsoft.Net.Http.Headers;
using MCPhappey.Tools.Deskbird;
using MCPhappey.Tools.Perplexity;

var builder = WebApplication.CreateBuilder(args);
var appConfig = builder.Configuration.Get<Config>();

var basePath = Path.Combine(AppContext.BaseDirectory, "Servers");
var servers = basePath.GetServers(appConfig?.Simplicate?.Organization ?? "").ToList();

if (!string.IsNullOrEmpty(appConfig?.McpDatabase))
{
    servers.AddRange(builder.AddSqlMcpServers(appConfig.McpDatabase));
}

if (appConfig?.McpExtensions != null)
{
    foreach (var server in servers.Where(s => !string.IsNullOrEmpty(s.Server.BaseMcp)))
    {
        if (appConfig.McpExtensions.TryGetValue(server.Server.BaseMcp!, out var ext))
        {
            server.Server.McpExtension = appConfig.McpExtensions[server.Server.BaseMcp!];
        }
    }
}

var deskbirdKey = appConfig?.DomainHeaders?
            .FirstOrDefault(a => a.Key == "connect.deskbird.com")
            .Value
            .FirstOrDefault(a => a.Key == HeaderNames.Authorization).Value.GetBearerToken();

if (deskbirdKey != null)
{
    builder.Services.AddSingleton(new DeskbirdSettings()
    {
        ApiKey = deskbirdKey
    });
}


var perplexityKey = appConfig?.DomainHeaders?
            .FirstOrDefault(a => a.Key == "api.perplexity.ai")
            .Value
            .FirstOrDefault(a => a.Key == HeaderNames.Authorization).Value.GetBearerToken();

if (perplexityKey != null)
{
    builder.Services.AddSingleton(new PerplexitySettings()
    {
        ApiKey = perplexityKey
    });
}

var apiKey = appConfig?.DomainHeaders?
            .FirstOrDefault(a => a.Key == Hosts.OpenAI)
            .Value
            .FirstOrDefault(a => a.Key == HeaderNames.Authorization).Value.GetBearerToken();

var openAiClient = !string.IsNullOrEmpty(apiKey) ?
    new OpenAIClient(apiKey) : null;

builder.Services.AddApplicationInsightsTelemetry();
builder.Logging.AddApplicationInsights();

if (!string.IsNullOrEmpty(appConfig?.KernelMemoryDatabase)
    && openAiClient != null
    && apiKey != null)
{
    builder.Services.AddKernelMemoryWithOptions(memoryBuilder =>
    {
        memoryBuilder
            .WithCustomWebScraper<DownloadService>()
            .WithSimpleQueuesPipeline()
            .WithOpenAI(new OpenAIConfig()
            {
                APIKey = apiKey,
                TextModel = "gpt-4.1",
                TextModelMaxTokenTotal = 65536,
                EmbeddingDimensions = 3072,
                EmbeddingModel = "text-embedding-3-large"
            })
            .WithDecoders(openAiClient)
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
            .AllowAnyMethod()
            .WithExposedHeaders("Mcp-Session-Id");
    });
});

builder.WithCompletion();

if (!string.IsNullOrEmpty(appConfig?.PrivateKey))
{
    builder.AddAuthServices(appConfig.PrivateKey);
}

if (appConfig?.ApplicationInsights != null)
{
    builder.Services.AddSingleton(appConfig.ApplicationInsights);
}

builder.Services.WithHostScrapers(appConfig?.DomainHeaders, appConfig?.DomainQueryStrings);

if (appConfig?.OAuth != null)
{
    builder.Services.WithOboScrapers(servers, appConfig.OAuth);
}



if (openAiClient != null)
{
    builder.Services.AddSingleton(openAiClient);
}

var googleApiKey = appConfig?.DomainQueryStrings?
            .FirstOrDefault(a => a.Key == "generativelanguage.googleapis.com")
            .Value
            .FirstOrDefault(a => a.Key == "key").Value.GetBearerToken();

if (googleApiKey != null)
{
    builder.WithGoogleAI(googleApiKey);
}

builder.Services.WithDefaultScrapers();

if (appConfig?.Simplicate != null)
{
    builder.WithSimplicateScraper(appConfig.Simplicate, appConfig.OAuth);
    servers.ApplySimplicateOrganization(appConfig.Simplicate.Organization);
}

if (appConfig?.Agent2AgentStorage != null)
{
    builder.WithAgent2Agent(appConfig.Agent2AgentStorage.ConnectionString,
        appConfig.Agent2AgentStorage.TaskContainer,
        appConfig.Agent2AgentStorage.ContextContainer,
        appConfig.Agent2AgentStorage.Database);
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
