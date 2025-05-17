using System.Security.Claims;
using MCPhappey.Auth.Models;
using MCPhappey.Common.Models;

namespace MCPhappey.Auth.Extensions;

public static class AspNetCoreWebAppExtensions
{
    public static void MapServerOAuth(
              this WebApplication webApp,
              string serverUrl,
              string[] scopes,
              string resource)
    {
        webApp.MapGet($"/.well-known/oauth-protected-resource{serverUrl}", (HttpContext context) =>
        {
            var request = context.Request;
            var baseUri = $"{request.Scheme}://{request.Host.Value}";

            return Results.Json(new
            {
                authorization_servers = new[]
                {
                            $"{baseUri}/.well-known/oauth-authorization-server"
                },
                resource,
                scopes_supported = scopes,
            });
        });
    }

    public static WebApplication MapOAuth(
            this WebApplication webApp,
            List<ServerConfig> servers,
            OAuthSettings oauthSettings)
    {
        webApp.UseAuthorization();
        webApp.MapAuth();

        webApp.Use(async (context, next) =>
        {
            var path = context.Request.Path.Value ?? "";
            if (path.Contains(".well-known", StringComparison.OrdinalIgnoreCase))
            {
                await next();
                return;
            }

            var validator = context.RequestServices.GetRequiredService<IJwtValidator>();
            var oAuthSettings = context.RequestServices.GetRequiredService<OAuthSettings>();
            var matchedServer = servers.FirstOrDefault(a =>
                path.StartsWith($"/{a.Server.ServerInfo.Name}", StringComparison.OrdinalIgnoreCase));

            if (matchedServer is null)
            {
                await next();
                return;
            }

            if (matchedServer.IsAuthorized(context.Request.Headers.ToDictionary(k => k.Key, v => v.Value.ToString())))
            {
                await next();
                return;
            }

            var token = context.GetBearerToken();
            if (string.IsNullOrEmpty(token))
            {
                await WriteUnauthorized(context, oAuthSettings);
                return;
            }

            var baseUrl = $"{context.Request.Scheme}://{context.Request.Host}";
            var principal = await validator.ValidateAsync(token, baseUrl,
                oAuthSettings.Audience, oAuthSettings);
                
            if (principal is null)
            {
                await WriteUnauthorized(context, oAuthSettings);
                return;
            }

            if (!IsOwnerOrGroupAuthorized(matchedServer, principal))
            {
                context.Response.StatusCode = 403;
                await context.Response.WriteAsync("Forbidden: user is not authorized for this server");
                return;
            }

            context.User = principal;

            // TODO: Add role support here if needed

            await next();
        });

        foreach (var server in servers)
        {
            webApp.MapServerOAuth("/" + server.Server.ServerInfo.Name.ToLowerInvariant(),
                oauthSettings.Scopes?.Split(" ") ?? [],
                oauthSettings.Audience);
        }

        return webApp;
    }

    static bool IsOwnerOrGroupAuthorized(ServerConfig matchedServer, ClaimsPrincipal principal)
    {
        var ownersValid = true;
        var groupsValid = true;

        if (matchedServer.Server.Owners?.Any() == true)
        {
            var oid = principal.FindFirst("oid")?.Value;
            ownersValid = !string.IsNullOrEmpty(oid)
                && matchedServer.Server.Owners.Contains(oid, StringComparer.OrdinalIgnoreCase);
        }

        if (matchedServer.Server.Groups?.Any() == true)
        {
            var userGroups = principal.Claims
                .Where(c => c.Type == "groups")
                .Select(c => c.Value)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            groupsValid = matchedServer.Server.Groups.Any(g => userGroups.Contains(g));
        }

        return ownersValid || groupsValid;
    }

    static async Task WriteUnauthorized(HttpContext context, OAuthSettings oAuthSettings)
    {
        var baseUrl = $"{context.Request.Scheme}://{context.Request.Host}";
        var resourceMetadata = $"{baseUrl}/.well-known/oauth-protected-resource{context.Request.Path}";
        var authorizationUri = $"{baseUrl}/authorize";

        context.Response.StatusCode = 401;
        context.Response.Headers.Append("WWW-Authenticate",
            $"Bearer resource_metadata=\"{resourceMetadata}\", " +
            $"authorization_uri=\"{authorizationUri}\", " +
            $"resource=\"{oAuthSettings.Audience}\"");

        await context.Response.WriteAsync("Invalid or missing token");
    }


}
