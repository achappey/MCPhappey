using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using MCPhappey.Core.Models.Protocol;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace MCPhappey.Core.Extensions;

public static class HttpExtensions
{
    public static async Task<string?> ExchangeOnBehalfOfTokenAsync(this IHttpClientFactory httpClientFactory,
       string incomingAccessToken,
       string clientId,
       string clientSecret,
       string tokenEndpoint,
       string[] scopes)
    {
        using var http = httpClientFactory.CreateClient();

        var body = new Dictionary<string, string>
            {
                { "client_id", clientId },
                { "client_secret", clientSecret },
                { "grant_type", "urn:ietf:params:oauth:grant-type:jwt-bearer" },
                { "requested_token_use", "on_behalf_of" },
                { "assertion", incomingAccessToken },
                { "scope", string.Join(" ", scopes) }
            };

        var response = await http.PostAsync(tokenEndpoint, new FormUrlEncodedContent(body));

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"OBO token exchange failed: {error}");
        }

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();

        return json.GetProperty("access_token").GetString();
    }

    public static string? GetBearerToken(this HttpContext httpContext)
        => httpContext.Request.Headers.Authorization.ToString()?.GetBearerToken();

    public static string? GetBearerToken(this string value)
        => value.Split(" ").LastOrDefault();

   /* public static async Task<GraphServiceClient> GetOboGraphClient(this IServiceProvider serviceProvider,
        IMcpServer mcpServer)
    {
        var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
        var contextAccess = serviceProvider.GetRequiredService<IHttpContextAccessor>();
        var servers = serviceProvider.GetRequiredService<List<ServerConfig>>();
        var serverConfig = servers.FirstOrDefault(a => a.Server.ServerInfo.Name.Equals(mcpServer.ServerOptions.ServerInfo?.Name,
            StringComparison.OrdinalIgnoreCase));

        return await httpClientFactory.GetOboGraphClient(contextAccess.HttpContext?.GetBearerToken()!,
                   serverConfig?.Auth);
    }*/

    public static string? GetUserId(this IServiceProvider serviceProvider)
    {
        var contextAccess = serviceProvider.GetRequiredService<IHttpContextAccessor>();

        return contextAccess.HttpContext?.GetObjectId();
    }

    public static async Task<HttpClient> GetOboHttpClient(this IHttpClientFactory httpClientFactory,
       string token,
       string host,
       ServerAuth? auth)
    {
        var delegated = await httpClientFactory.GetOboToken(token, host, auth);

        var httpClient = httpClientFactory.CreateClient();
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", delegated);
        return httpClient;
    }

    public static async Task<string> GetOboToken(this IHttpClientFactory httpClientFactory,
      string token,
      string host,
      ServerAuth? auth)
    {
        if (auth?.OBO?.ContainsKey(host) == true)
        {
            var oboConfig = auth.OBO[host] ?? throw new Exception();

            var delegated = await httpClientFactory.ExchangeOnBehalfOfTokenAsync(token,
                        oboConfig.ClientId, oboConfig.ClientSecret, oboConfig.TokenEndpoint, oboConfig.Scopes);

            return delegated ?? throw new UnauthorizedAccessException();
        }

        throw new UnauthorizedAccessException();
    }

    public static string? GetObjectId(this HttpContext context)
    {
        return context.User?.FindFirst("oid")?.Value;
    }
}