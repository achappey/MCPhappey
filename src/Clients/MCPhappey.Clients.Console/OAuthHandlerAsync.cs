using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Web;
using Microsoft.AspNetCore.WebUtilities;
using ModelContextProtocol.Client;
using MCPhappey.Common.Models;
using System.Net.Http.Json;
using Microsoft.Net.Http.Headers;

namespace MCPhappey.Console;

public static class OAuthHandlerAsync
{
    private static readonly Dictionary<string, string> TokenCache = [];

    public static async Task<IMcpClient> Connect(
           MCPServer server,
           McpClientOptions opts,
           HttpClient http)
    {
        /* --- 1. Discover resource metadata --- */
        var baseUri = new Uri(server.Url);
        var prUrl = $"{baseUri.Scheme}://{baseUri.Host}:{baseUri.Port}/.well-known/oauth-protected-resource{baseUri.AbsolutePath}";

        using var pr = await http.GetAsync(prUrl);
        pr.EnsureSuccessStatusCode();

        using var prDoc = JsonDocument.Parse(await pr.Content.ReadAsStreamAsync());
        string resource = prDoc.RootElement.GetProperty("resource").GetString()!;
        var scopes = prDoc.RootElement.GetProperty("scopes_supported")
                         .EnumerateArray();
        string scope = string.Join(' ', scopes);

        if (TokenCache.TryGetValue(resource, out var cachedToken))
            return await BuildClient(server.Url, cachedToken, opts);

        var authUrl = prDoc.RootElement.GetProperty("authorization_servers")[0].GetString()!;
        var metaUrl = $"{authUrl}";

        /* --- 2. Get auth server metadata --- */
        var metaDoc = await http.GetFromJsonAsync<JsonDocument>(metaUrl);
        var tokenEndpoint = metaDoc!.RootElement.GetProperty("token_endpoint").GetString()!;
        var authorizationEndpoint = metaDoc.RootElement.GetProperty("authorization_endpoint").GetString()!;
        var registrationEndpoint = metaDoc.RootElement.GetProperty("registration_endpoint").GetString()!;
        int port = GetFreePort();
        string redirect = $"http://localhost:{port}/";

        /* --- 3. Dynamic client registration --- */
        var clientReg = new
        {
            client_name = "MCP Console Tester",
            redirect_uris = new[] { redirect }, // dummy placeholder for now
            grant_types = new[] { "authorization_code" },
            response_types = new[] { "code" },
            token_endpoint_auth_method = "none"
        };

        var regResponse = await http.PostAsJsonAsync(registrationEndpoint, clientReg);
        regResponse.EnsureSuccessStatusCode();

        var regJson = await regResponse.Content.ReadFromJsonAsync<JsonDocument>();
        var clientId = regJson!.RootElement.GetProperty("client_id").GetString()!;

        /* --- 4. PKCE setup --- */
        string verifier = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        string challenge = WebEncoders.Base64UrlEncode(
                               SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));
        string state = Guid.NewGuid().ToString("N");

        using var listener = new HttpListener();
        listener.Prefixes.Add(redirect);
        listener.Start();

        /* --- 5. Open browser --- */
        var finalAuthUrl =
            $"{authorizationEndpoint}?" +
            $"client_id={Uri.EscapeDataString(clientId)}" +
            $"&response_type=code" +
            $"&redirect_uri={Uri.EscapeDataString(redirect)}" +
            $"&code_challenge={Uri.EscapeDataString(challenge)}" +
            $"&code_challenge_method=S256" +
            $"&scope={Uri.EscapeDataString(scope)}" +
            $"&state={Uri.EscapeDataString(state)}";

        // Console.WriteLine($"🔑 Open in browser: {finalAuthUrl}");
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = finalAuthUrl,
            UseShellExecute = true
        });

        /* --- 6. Wait for callback --- */
        var ctx = listener.GetContext();
        var q = HttpUtility.ParseQueryString(ctx.Request.Url!.Query);
        listener.Stop();

        if (q["error"] is string err)
            throw new Exception($"OAuth error: {err}");

        if (q["state"] != state || q["code"] is not string code)
            throw new InvalidOperationException("State mismatch / no code");

        /* --- 7. Exchange code for token --- */
        var tokenReq = new FormUrlEncodedContent(new Dictionary<string, string?>
        {
            { "client_id", clientId },
            { "grant_type", "authorization_code" },
            { "code", code },
            { "redirect_uri", redirect },
            { "code_verifier", verifier }
        });

        var tokenResp = await http.PostAsync(tokenEndpoint, tokenReq);
        string tokenBody = await tokenResp.Content.ReadAsStringAsync();

        if (!tokenResp.IsSuccessStatusCode)
            throw new Exception($"Token error: {tokenBody}");

        string access = JsonDocument.Parse(tokenBody).RootElement.GetProperty("access_token").GetString()!;
        TokenCache[resource] = access;

        return await BuildClient(server.Url, access, opts);
    }

    /* ---------- helpers ---------- */
    private static async Task<IMcpClient> BuildClient(string url, string bearer, McpClientOptions opts)
    {
        var transportOptions = new SseClientTransportOptions
        {
            Endpoint = new Uri(url),   // URL of the MCP “/mcp” endpoint
            UseStreamableHttp = true,                      // <-- enable the new transport
            Name = "streamable-http",          // (optional) nice display name
            AdditionalHeaders = new() { [HeaderNames.Authorization] = "Bearer " + bearer }
        };

        // 2. Create the transport – **still** SseClientTransport, just with the flag above
        IClientTransport clientTransport = new SseClientTransport(transportOptions);
        var t2 = new SseClientTransport(new()
        {
            Endpoint = new Uri(url),
            AdditionalHeaders = new() { [HeaderNames.Authorization] = "Bearer " + bearer }
        });
        return await McpClientFactory.CreateAsync(clientTransport, opts);
    }

    private static int GetFreePort()
    {
        using var l = new TcpListener(IPAddress.Loopback, 0);
        l.Start();
        return ((IPEndPoint)l.LocalEndpoint).Port;
    }
}
