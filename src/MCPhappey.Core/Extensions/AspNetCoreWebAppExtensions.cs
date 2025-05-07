
using MCPhappey.Core.Auth;
using MCPhappey.Core.Models.Protocol;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace MCPhappey.Core.Extensions;

public static class AspNetCoreWebAppExtensions
{
    private static readonly string[] grantTypesSupported = [ "authorization_code", "client_credentials",
        "urn:ietf:params:oauth:grant-type:jwt-bearer" ];
    private static readonly string[] responseTypesSupported = ["code", "token"];
    private static readonly string[] tokenEndpointAuthMethodsSupported = ["client_secret_post"];
    private static readonly string[] claimsSupported = ["sub", "iss", "aud", "scp", "roles", "oid"];

    public static void AddMcpServer(
              this WebApplication webApp,
              ServerConfig server)
    {
        var prefix = server.Server.GetServerRelativeUrl();
        var mcpGroup = webApp.MapMcp(prefix);

        webApp.MapGet($"{prefix}/.well-known/oauth-protected-resource", () =>
        {
            return Results.Json(new
            {
                authorization_servers = new[]
                {
                            $"{prefix}/.well-known/oauth-authorization-server"
                },
                resource = server.Auth?.OAuth?.Audience,
                scopes_supported = server.Auth?.OAuth?.Scopes,
                issuer = server.Auth?.Issuer
            });
        });

        webApp.MapGet($"{prefix}/.well-known/oauth-authorization-server", () =>
        {
            if (server.Auth is null)
                return Results.NotFound();

            return Results.Json(new
            {
                issuer = server.Auth.Issuer,
                authorization_endpoint = server.Auth.AuthorizationEndpoint,
                token_endpoint = server.Auth.TokenEndpoint,
                jwks_uri = server.Auth.JwksUri,
                scopes_supported = server.Auth.OAuth?.Scopes,
                response_types_supported = responseTypesSupported,
                grant_types_supported = grantTypesSupported,
                token_endpoint_auth_methods_supported = tokenEndpointAuthMethodsSupported,
                claims_supported = claimsSupported
            });
        });

        // 2. Conditionally require authorization if Auth is configured
        if (server.Auth is not null)
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
        foreach (var server in servers)
        {
            webApp.AddMcpServer(server);
        }

        webApp.MapGet("/mcp.json", (HttpContext ctw)
                   => Results.Json(servers.ToMcpServerList(ctw)));

        webApp.Use(async (context, next) =>
        {
            var path = context.Request.Path.Value ?? "";
            if (path.Contains(".well-known", StringComparison.OrdinalIgnoreCase))
            {
                await next();
                return;
            }

            var validator = context.RequestServices.GetRequiredService<IJwtValidator>();

            var matchedServer = servers.FirstOrDefault(s =>
                path.StartsWith(s.Server.GetServerRelativeUrl(), StringComparison.OrdinalIgnoreCase));

            if (matchedServer?.Auth is not null)
            {
                var token = context.GetBearerToken();
                if (!string.IsNullOrEmpty(token))
                {
                    var principal = await validator.ValidateAsync(token, matchedServer);

                    if (principal is null)
                    {
                        context.Response.StatusCode = 401;
                        context.Response.Headers.WWWAuthenticate = $"Bearer authorization_uri=\"{context.Request.Scheme}://{context.Request.Host}{matchedServer.Server.GetServerRelativeUrl()}/.well-known/oauth-protected-resource\"";

                        await context.Response.WriteAsync("Invalid or expired token");
                        return;
                    }

                    context.User = principal;

                    if (matchedServer.Auth.OAuth?.RequiredRoles is { Length: > 0 })
                    {
                        var userRoles = context.User.FindAll("roles").Select(r => r.Value);
                        if (!matchedServer.Auth.OAuth.RequiredRoles.Any(required => userRoles.Contains(required)))
                        {
                            context.Response.StatusCode = 403;
                            await context.Response.WriteAsync("Forbidden: missing required role");
                            return;
                        }
                    }
                }
                else
                {
                    context.Response.StatusCode = 401;
                    context.Response.Headers.WWWAuthenticate = $"Bearer authorization_uri=\"{context.Request.Scheme}://{context.Request.Host}{matchedServer.Server.GetServerRelativeUrl()}/.well-known/oauth-protected-resource\"";

                    await context.Response.WriteAsync("Missing Bearer token");
                    return;
                }
            }

            await next();
        });

        return webApp;
    }
}