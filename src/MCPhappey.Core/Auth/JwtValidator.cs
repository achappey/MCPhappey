

using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;
using System.Security.Claims;
using MCPhappey.Core.Models.Protocol;
using Microsoft.IdentityModel.Tokens;

namespace MCPhappey.Core.Auth;

public interface IJwtValidator
{
    Task<ClaimsPrincipal?> ValidateAsync(string token, ServerConfig serverConfig);
}

public class JwtValidator : IJwtValidator
{
    private readonly IHttpClientFactory _httpClientFactory;

    public JwtValidator(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<ClaimsPrincipal?> ValidateAsync(string token, ServerConfig config)
    {
        var client = _httpClientFactory.CreateClient();
        var jwks = await client.GetFromJsonAsync<JsonWebKeySet>(config.Auth!.JwksUri);

        var validationParameters = new TokenValidationParameters
        {
            ValidAudiences = [config.Auth.OAuth?.Audience],
            ValidIssuers = config.Auth.ValidIssuers,
            IssuerSigningKeys = jwks?.Keys,
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };

        var handler = new JwtSecurityTokenHandler();
        try
        {
            var result = await handler.ValidateTokenAsync(token, validationParameters);

            return new ClaimsPrincipal(result.ClaimsIdentity);
        }
        catch
        {
            return null;
        }
    }
}
