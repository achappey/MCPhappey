using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Text.Json;
using MCPhappey.Auth.Models;
using MCPhappey.Common;
using MCPhappey.Common.Constants;
using MCPhappey.Common.Models;

namespace MCPhappey.Auth.Extensions;

public static class HttpExtensions
{

  public static string? GetBearerToken(this HttpContext httpContext)
    => httpContext.Request.Headers.Authorization.ToString()?.GetBearerToken();

  public static string? GetBearerToken(this string value)
      => value.Split(" ").LastOrDefault();

  public static string? GetActClaimFromMcpToken(string rawMcpToken)
  {
    var handler = new JwtSecurityTokenHandler();

    // Decode the token (without validating signature)
    var jwt = handler.ReadJwtToken(rawMcpToken);

    // Extract the 'act' claim
    return jwt.Claims.FirstOrDefault(c => c.Type == "act")?.Value;
  }

  public static async Task<string?> ExchangeOnBehalfOfTokenAsync(this IHttpClientFactory httpClientFactory,
     string incomingAccessToken,
     string clientId,
     string clientSecret,
     string tokenEndpoint,
     string[] scopes)
  {
    using var http = httpClientFactory.CreateClient();
    string? azureAccessToken = GetActClaimFromMcpToken(incomingAccessToken);

    if (string.IsNullOrEmpty(azureAccessToken))
      throw new Exception("No 'act' claim found in MCP token");

    var body = new Dictionary<string, string>
            {
                { "client_id", clientId },
                { "client_secret", clientSecret },
                { "grant_type", "urn:ietf:params:oauth:grant-type:jwt-bearer" },
                { "requested_token_use", "on_behalf_of" },
                { "assertion", azureAccessToken },
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

  public static string? GetUserId(this IServiceProvider serviceProvider)
  {
    var token = serviceProvider.GetRequiredService<HeaderProvider>();
    var outerJwt = new JwtSecurityTokenHandler().ReadJwtToken(token.Bearer);
    var userId = outerJwt.Claims.FirstOrDefault(c => c.Type == "oid")?.Value;

    return userId;
  }

  public static async Task<HttpClient> GetOboHttpClient(this IHttpClientFactory httpClientFactory,
     string token,
     string host,
     Server server,
     OAuthSettings oAuthSettings)
  {
    var delegated = await httpClientFactory.GetOboToken(token, host, server, oAuthSettings);

    var httpClient = httpClientFactory.CreateClient();
    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", delegated);
    return httpClient;
  }

  public static async Task<string> GetOboToken(this IHttpClientFactory httpClientFactory,
    string token,
    string host,
    Server server,
    OAuthSettings oAuthSettings)
  {
    if (server.OBO?.ContainsKey(host) == true)
    {
      var delegated = await httpClientFactory.ExchangeOnBehalfOfTokenAsync(token,
                  oAuthSettings.ClientId, oAuthSettings.ClientSecret,
                  $"https://login.microsoftonline.com/{oAuthSettings.TenantId}/oauth2/v2.0/token",
                  server.OBO.GetScopes(host)?.ToArray() ?? []);

      return delegated ?? throw new UnauthorizedAccessException();
    }

    throw new UnauthorizedAccessException();
  }

  public static string? GetObjectId(this HttpContext context)
  {
    return context.User?.FindFirst("oid")?.Value;
  }

  public static IEnumerable<string>? GetScopes(this Dictionary<string, string>? metadata, string host)
       => metadata?.ContainsKey(host) == true ? metadata[host].ToString().Split(" ") : null;

}