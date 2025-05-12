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
            var matchedServer = servers.Any(a => context.Request.Path.Value?.StartsWith($"/{a.Server.ServerInfo.Name}", StringComparison.OrdinalIgnoreCase) == true);

            if (matchedServer)
            {
                var token = context.GetBearerToken();
                if (!string.IsNullOrEmpty(token))
                {
                    var baseUrl = $"{context.Request.Scheme}://{context.Request.Host}";
                    var principal = await validator.ValidateAsync(token, baseUrl, oAuthSettings.Audience, oAuthSettings);

                    if (principal is null)
                    {
                        context.Response.StatusCode = 401;
                        var resourceMetadata = $"{baseUrl}/.well-known/oauth-protected-resource{context.Request.Path}";
                        var authorizationUri = $"{baseUrl}/authorize";

                        context.Response.Headers.Append("WWW-Authenticate",
                                                    $"Bearer resource_metadata=\"{resourceMetadata}\", " +
                                                    $"authorization_uri=\"{authorizationUri}\", " +
                                                    $"resource=\"{oauthSettings.Audience}\"");

                        await context.Response.WriteAsync("Invalid or expired token");
                        return;
                    }

                    context.User = principal;

                    // TODO ADD ROLE SUPPORT
                    /*   if (matchedServer.Auth.OAuth?.RequiredRoles is { Length: > 0 })
                       {
                           var userRoles = context.User.FindAll("roles").Select(r => r.Value);
                           if (!matchedServer.Auth.OAuth.RequiredRoles.Any(required => userRoles.Contains(required)))
                           {
                               context.Response.StatusCode = 403;
                               await context.Response.WriteAsync("Forbidden: missing required role");
                               return;
                           }
                       }*/
                }
                else
                {
                    context.Response.StatusCode = 401;

                    var baseUrl = $"{context.Request.Scheme}://{context.Request.Host}";
                    var resourceMetadata = $"{baseUrl}/.well-known/oauth-protected-resource{context.Request.Path}";
                    var authorizationUri = $"{baseUrl}/authorize";

                    context.Response.Headers.Append("WWW-Authenticate",
                        $"Bearer resource_metadata=\"{resourceMetadata}\", " +
                        $"authorization_uri=\"{authorizationUri}\", " +
                        $"resource=\"{oauthSettings.Audience}\"");

                    await context.Response.WriteAsync("Missing Bearer token");
                    return;
                }
            }

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

}
