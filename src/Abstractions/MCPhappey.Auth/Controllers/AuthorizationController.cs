
using MCPhappey.Auth.Cache;
using MCPhappey.Auth.Models;
using Microsoft.AspNetCore.WebUtilities;

namespace MCPhappey.Auth.Controllers;

public static class AuthorizationController
{
    public static async Task<IResult> Handle(HttpContext ctx, OAuthSettings oauth)
    {
        var req = ctx.Request;

        var codeChallenge = req.Query["code_challenge"].ToString();
        var incomingClientId = req.Query["client_id"].ToString();
        var originalRedirectUri = req.Query["redirect_uri"].ToString();
        var state = req.Query["state"].ToString();
        var scope = req.Query["scope"].ToString();

        if (string.IsNullOrEmpty(incomingClientId) || string.IsNullOrEmpty(originalRedirectUri))
            return Results.BadRequest("Missing client_id or redirect_uri");

        if (string.IsNullOrEmpty(state))
            state = Guid.NewGuid().ToString("N");

        if (string.IsNullOrEmpty(scope))
            scope = string.Join(" ", oauth.Scopes?.Split(" ") ?? []);

        // Save mapping from state → redirect_uri
        PkceCache.Store(state, originalRedirectUri);

        var serverRedirectUri = $"{req.Scheme}://{req.Host}/callback";

        var azureAuthUrl = QueryHelpers.AddQueryString(
            $"https://login.microsoftonline.com/{oauth.TenantId}/oauth2/v2.0/authorize",
            new Dictionary<string, string?>
            {
                ["client_id"] = oauth.ClientId,
                ["response_type"] = "code",
                ["redirect_uri"] = serverRedirectUri,
                ["scope"] = scope,
                ["code_challenge"] = codeChallenge,
                ["code_challenge_method"] = "S256",
                ["state"] = state
            });

        return await Task.FromResult(Results.Redirect(azureAuthUrl));
    }
}
