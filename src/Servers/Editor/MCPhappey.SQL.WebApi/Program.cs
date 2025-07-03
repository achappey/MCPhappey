using MCPhappey.Sql.WebApi;
using MCPhappey.SQL.WebApi.Context;
using Microsoft.EntityFrameworkCore;
using MCPhappey.SQL.WebApi.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using MCPhappey.SQL.WebApi.Services;
using MCPhappey.SQL.WebApi.Endpoints;

var builder = WebApplication.CreateBuilder(args);
var appConfig = builder.Configuration.Get<Config>();

builder.Services.AddDbContext<McpDatabaseContext>(options =>
           options.UseSqlServer(appConfig?.McpDatabase, sqlOpts => sqlOpts.EnableRetryOnFailure()));
builder.Services.AddScoped<ServerRepository>();
builder.Services.AddScoped<PromptRepository>();
builder.Services.AddScoped<ResourceRepository>();
builder.Services.AddScoped<ServerService>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        builder.Configuration.Bind("AzureAd", options);
        // Or manually set: options.Authority, options.Audience, etc.
    });

builder.Services.AddAuthorization(); // Default policy

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


var app = builder.Build();
app.UseCors("AllowSpecificOrigin");
app.UseRouting();

app.MapPromptEndpoints()
    .MapResourceEndpoints()
    .MapServerEndpoints();

//.MapResourceEndpoints()
//.MapResourceTemplateEndpoints();

app.Run();
