using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using MCPhappey.Auth.Cache;
using MCPhappey.Auth.Models;
using Microsoft.IdentityModel.Tokens;

namespace MCPhappey.Auth;

public interface IJwtValidator
{
    Task<ClaimsPrincipal?> ValidateAsync(string token, string issuer,
        string audience, OAuthSettings oAuthSettings);
}

public class JwtValidator(IHttpClientFactory httpClientFactory) : IJwtValidator
{

    public async Task<ClaimsPrincipal?> ValidateAsync(string token, string issuer,
        string audience, OAuthSettings oAuthSettings)
    {
        using var client = httpClientFactory.CreateClient();

        var keys = await JwksCache.GetAsync($"{issuer}/.well-known/jwks.json", httpClientFactory);
        if (keys == null || keys.Count == 0) return null;

        var handler = new JwtSecurityTokenHandler();

        // STEP 1: Validate the outer (MCP) token
        var outerValidationParameters = new TokenValidationParameters
        {
            ValidIssuers = [issuer],
            IssuerSigningKeys = keys,
            ValidateIssuer = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.FromMinutes(1),
            ValidateAudience = true,
            AudienceValidator = (tokenAudiences, _, _) =>
                tokenAudiences.Contains(audience, StringComparer.OrdinalIgnoreCase) == true
        };

        try
        {
            var outerResult = await handler.ValidateTokenAsync(token, outerValidationParameters);

            if (!outerResult.IsValid)
            {

            }

            var outerIdentity = outerResult.ClaimsIdentity;
            var principal = new ClaimsPrincipal(outerIdentity);

            // STEP 2: Check if token has embedded Azure token in `act` claim
            var actToken = outerIdentity.FindFirst("act")?.Value;

            if (!string.IsNullOrEmpty(actToken))
            {
                var innerJwt = handler.ReadJwtToken(actToken);
                var innerIssuer = innerJwt.Issuer;

                var innerKeys = await JwksCache.GetAsync($"https://login.microsoftonline.com/{oAuthSettings.TenantId}/discovery/v2.0/keys", httpClientFactory);
                if (innerKeys == null || innerKeys.Count == 0) return null;

                var innerValidation = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuers =
                    [
                        $"https://sts.windows.net/{oAuthSettings.TenantId}/",
                        $"https://login.microsoftonline.com/{oAuthSettings.TenantId}/v2.0"
                    ],
                    IssuerSigningKeys = innerKeys,
                    ValidateLifetime = true,
                    ValidateAudience = true,
                    AudienceValidator = (tokenAudiences, _, _) =>
                        tokenAudiences.Contains(audience, StringComparer.OrdinalIgnoreCase) == true
                };

                var innerResult = await handler.ValidateTokenAsync(actToken, innerValidation);
                var innerIdentity = innerResult.ClaimsIdentity;

                outerIdentity.AddClaims(innerIdentity.Claims
                    .Where(c => !outerIdentity.HasClaim(c.Type, c.Value))); // avoid duplicates
            }

            return principal;

        }
        catch
        {
            return null;
        }
    }
}
