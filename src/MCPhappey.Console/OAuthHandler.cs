using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Web;
using MCPhappey.Core.Models.Protocol;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol.Transport;

public static class OAuthHandler
{
    public static async Task<IMcpClient?> OAuthFallback(MCPServer server, McpClientOptions mcpClientOptions, AppSettings appSettings)
    {
        var protectedResourceUri = $"{server.Url.Replace("/sse", "")}/.well-known/oauth-protected-resource";
        using var http = new HttpClient();

        var resourceResponse = await http.GetAsync(protectedResourceUri);
        var resourceJson = await resourceResponse.Content.ReadAsStringAsync();
        var resourceDoc = JsonDocument.Parse(resourceJson).RootElement;

        var resource = resourceDoc.GetProperty("resource").GetString();
        var scopesSupported = resourceDoc.GetProperty("scopes_supported").EnumerateArray().Select(x => x.GetString()).Where(x => x != null).ToList();

        var scope = string.Join(" ", scopesSupported.Select(s => $"{resource}/{s}")) + " offline_access";
        var state = Guid.NewGuid().ToString("N");
        int port = GetRandomUnusedPort();
        string redirectUri = $"http://localhost:{port}/";

        // STEP 2: Build and launch browser
        string authUrl = $"{appSettings?.AuthHost}/authorize?" +
            $"client_id={HttpUtility.UrlEncode(appSettings?.ClientId)}" +
            $"&response_type=code" +
            $"&redirect_uri={HttpUtility.UrlEncode(redirectUri)}" +
            $"&response_mode=query" +
            $"&scope={HttpUtility.UrlEncode(scope)}" +
            $"&state={state}";

        ConsoleWriter.WriteInColor("🔑 Opening browser for authorization...", ConsoleColor.Yellow);
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = authUrl,
            UseShellExecute = true
        });

        // STEP 3: Start HTTP listener to catch redirect
        string? authCode = null;
        using var listener = new HttpListener();
        listener.Prefixes.Add(redirectUri);
        listener.Start();
        var context = await listener.GetContextAsync();
        var query = HttpUtility.ParseQueryString(context.Request.Url?.Query ?? "");
        authCode = query["code"];
        var stateReturned = query["state"];
        var response = context.Response;

        if (authCode == null)
        {
            response.StatusCode = 400;
            await response.OutputStream.WriteAsync(System.Text.Encoding.UTF8.GetBytes("Authorization failed"));
            response.Close();
            ConsoleWriter.WriteInColor("❌ No code received in redirect.", ConsoleColor.Red);
            return null;
        }

        response.StatusCode = 200;
        var html = "<html><body><h1>You can return to the console. Authorization succeeded.</h1></body></html>";
        var buffer = System.Text.Encoding.UTF8.GetBytes(html);
        await response.OutputStream.WriteAsync(buffer);
        response.Close();
        ConsoleWriter.WriteInColor("✔ Authorization code received!", ConsoleColor.Green);

        var tokenRequest = new FormUrlEncodedContent(new Dictionary<string, string?>
            {
                { "client_id", appSettings?.ClientId },
                { "client_secret", appSettings?.ClientSecret },
                { "grant_type", "authorization_code" },
                { "code", authCode },
                { "redirect_uri", redirectUri }
            });

        var tokenResponse = await http.PostAsync($"{appSettings?.AuthHost}/token", tokenRequest);
        var tokenJson = await tokenResponse.Content.ReadAsStringAsync();

        if (!tokenResponse.IsSuccessStatusCode)
        {
            ConsoleWriter.WriteInColor("❌ Failed to retrieve access token.", ConsoleColor.Red);
            ConsoleWriter.WriteInColor(tokenJson, ConsoleColor.DarkRed);
            return null;
        }

        var tokenDoc = JsonDocument.Parse(tokenJson);
        var accessToken = tokenDoc.RootElement.GetProperty("access_token").GetString();

        var clientTransport = new SseClientTransport(new()
        {
            Endpoint = new Uri(server.Url),
            AdditionalHeaders = new() {
                    {"Authorization", "Bearer " + accessToken}
              }
        });

        return await McpClientFactory.CreateAsync(clientTransport, mcpClientOptions);
    }

    static int GetRandomUnusedPort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }
}