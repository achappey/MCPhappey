
using System.Security.Cryptography;
using MCPhappey.Auth.Models;
using Microsoft.IdentityModel.Tokens;

namespace MCPhappey.Auth.Extensions;

public static class AspNetCoreExtensions
{
    public static IServiceCollection AddAuthServices(
        this WebApplicationBuilder builder,
        string privateKey,
        Dictionary<string, Dictionary<string, string>>? domainHeaders = null)
    {

        var rsa = RSA.Create();
        rsa.ImportFromPem(privateKey);
        var key = new RsaSecurityKey(rsa) { KeyId = "mcp-keyId" };
        var creds = new SigningCredentials(key, SecurityAlgorithms.RsaSha256);
        builder.Services.AddSingleton(creds);
        builder.Services.AddSingleton(key);

        builder.Services.AddAuthorization();
        builder.Services.AddHttpContextAccessor();

        builder.Services.AddSingleton<IJwtValidator, JwtValidator>();

        if (domainHeaders != null)
        {
            builder.Services.AddSingleton(domainHeaders);
        }

        builder.Services.AddScoped<TokenProvider>();
        builder.Services.AddHttpClient();

        return builder.Services;
    }
}