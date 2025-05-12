
using MCPhappey.Common.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace MCPhappey.Core.Extensions;

public static class AspNetCoreWebAppExtensions
{
    public static void AddMcpServer(
              this WebApplication webApp,
              ServerConfig server)
    {
        var prefix = server.Server.GetServerRelativeUrl();
        var mcpGroup = webApp.MapMcp(prefix);

        if (server.Server.HasAuth())
        {
            mcpGroup.RequireAuthorization(policyBuilder =>
            {
                policyBuilder.RequireAssertion(_ => true); // Placeholder for full policy
            });
        }
    }

    public static WebApplication UseMcpWebApplication(
            this WebApplication webApp,
            List<ServerConfig> servers)
    {
        webApp.MapGet("/mcp.json", (HttpContext context)
                   => Results.Json(servers.ToMcpServerList($"{context.Request.Scheme}://{context.Request.Host}")));

        webApp.MapGet("/mcp_sse.json", (HttpContext context)
                   => Results.Json(servers.ToMcpServerList($"{context.Request.Scheme}://{context.Request.Host}",
                      sse: true)));

        webApp.MapGet("/mcp_settings.json", (HttpContext context)
                   => Results.Json(servers.ToMcpServerSettingsList($"{context.Request.Scheme}://{context.Request.Host}")));

        webApp.MapGet("/gradio.json", (HttpContext ctw)
                   => Results.Json(servers.Select(a => a.ToGradio(ctw))));

        foreach (var server in servers)
        {
            webApp.AddMcpServer(server);
        }

        return webApp;
    }
}