using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using MCPhappey.Auth.Cache;
using MCPhappey.Auth.Models;
using Microsoft.IdentityModel.Tokens;

namespace MCPhappey.Auth.Controllers;

public static class TokenController
{

    public static string CreateJwt(string issuer, string subject, string audience, IEnumerable<string> scopes,
        SigningCredentials signingCredentials,
        IDictionary<string, object>? additionalClaims = null,
        DateTime? expires = null)
    {
        var handler = new JwtSecurityTokenHandler();
        var claims = new List<Claim>
    {
        new("sub", subject),
        new("scp", string.Join(" ", scopes)),
        new("iat", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()),
    };

        if (additionalClaims != null)
        {
            foreach (var pair in additionalClaims)
            {
                claims.Add(new Claim(pair.Key, pair.Value.ToString()!));
            }
        }

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expires ?? DateTime.UtcNow.AddHours(1),
            signingCredentials: signingCredentials

        );

        return handler.WriteToken(token);
    }
 
    public static async Task<IResult> Handle(
        HttpContext ctx,
        IHttpClientFactory httpClientFactory,
        OAuthSettings oauth,
        SigningCredentials signingCredentials)
    {
        var form = await ctx.Request.ReadFormAsync();

        var code = form["code"].ToString();
        var codeVerifier = form["code_verifier"].ToString();
        var grantType = form["grant_type"].ToString();

        if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(codeVerifier))
        {
            return Results.BadRequest("Missing required parameters.");
        }

        // Look up original redirect_uri (saved in /callback)
        var redirectUri = CodeCache.Retrieve(code);
        if (string.IsNullOrEmpty(redirectUri))
        {
            return Results.BadRequest("Unknown or expired code");
        }

        if (grantType == "client_credentials")
        {
            var clientId = form["client_id"].ToString();
            var clientSecret = form["client_secret"].ToString();
            var scope = form["scope"].ToString();

            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
                return Results.BadRequest("Missing client_id or client_secret");

            // ✅ Look up client in your confidential clients registry
            if (!oauth.ConfidentialClients?.TryGetValue(clientId, out var conf) == true)
                return Results.BadRequest("Unknown confidential client");
           
            return Results.Json(new
            {
                access_token = CreateJwt($"{ctx.Request.Scheme}://{ctx.Request.Host}", clientId,
                  oauth.Audience, oauth.Scopes.Split(" ") ?? [], signingCredentials,
                additionalClaims: new Dictionary<string, object>()),
                token_type = "Bearer",
                expires_in = 3600
            });
        }

        // Must match redirect_uri used during /authorize
        var tokenRequest = new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["client_id"] = oauth.ClientId,
            ["client_secret"] = oauth.ClientSecret,
            ["code"] = code,
            ["redirect_uri"] = $"{ctx.Request.Scheme}://{ctx.Request.Host}/callback",
            ["code_verifier"] = codeVerifier,
            ["scope"] = string.Join(" ", oauth.Scopes.Split(" ") ?? [])
        };

        var client = httpClientFactory.CreateClient();
        var response = await client.PostAsync(
            $"https://login.microsoftonline.com/{oauth.TenantId}/oauth2/v2.0/token",
            new FormUrlEncodedContent(tokenRequest));

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync();
            return Results.BadRequest(errorBody);
        }

        var azureToken = await response.Content.ReadFromJsonAsync<JsonElement>();
        var azureAccessToken = azureToken.GetProperty("access_token").GetString();

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(azureAccessToken);
        var sub = jwt.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
        var azureExp = jwt.ValidTo;
        var oid = jwt.Claims.FirstOrDefault(c => c.Type == "oid")?.Value;
        // ⏱ Optional: subtract a few minutes to be safe
        var exp = azureExp.AddMinutes(-2);

        var baseUri = $"{ctx.Request.Scheme}://{ctx.Request.Host}";
        var mcpToken = CreateJwt(baseUri, sub!, oauth.Audience, scopes: oauth.Scopes?.Split(" ") ?? [], signingCredentials,
            additionalClaims: new Dictionary<string, object>
            {
                ["act"] = azureAccessToken!,
                ["oid"] = oid!
            }, expires: exp);

        return Results.Json(new
        {
            access_token = mcpToken,
            token_type = "Bearer",
            expires_in = 3600
        });
    }
}