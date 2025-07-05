using Azure.Security.KeyVault.Secrets;
using MCPhappey.Common;
using MCPhappey.Common.Models;
using MCPhappey.Simplicate.Options;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Server;
using MCPhappey.Common.Extensions;
using System.Web;
using System.Text.Json;
using MCPhappey.Auth.Extensions;

namespace MCPhappey.Simplicate;

//
// Summary:
//     IContentScraper implementation that retrieves Simplicate files.
//
// Details:
//     • Determines per-user Simplicate API credentials by reading a secret from
//       Azure Key Vault where the secret-name equals the user’s OID (object id).
//     • Adds required „Authentication-Key” & „Authentication-Secret” headers.
//     • Downloads the requested resource and converts it to FileItem.
//
public class SimplicateScraper(
    IHttpClientFactory httpClientFactory,
    SimplicateOptions simplicateOptions,
    SecretClient? secretClient) : IContentScraper
{
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly SecretClient? _secretClient = secretClient;

    public bool SupportsHost(ServerConfig currentConfig, string host)
        => new Uri(host).Host == $"{simplicateOptions.Organization}.simplicate.app";

    public async Task<IEnumerable<FileItem>?> GetContentAsync(
        IMcpServer mcpServer,
        IServiceProvider serviceProvider,
        string url,
        CancellationToken cancellationToken = default)
    {
        var tokenProvider = serviceProvider.GetService<HeaderProvider>();
        var (key, secret) = await TryGetKeySecretAsync(tokenProvider, cancellationToken);
        if (key is null || secret is null)
            return null;

        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Add("Authentication-Key", key);
        client.DefaultRequestHeaders.Add("Authentication-Secret", secret);

        using var response = await client.GetAsync(url, cancellationToken);

        if (url.Contains(".simplicate.app/api/v2/storage/loadfile", StringComparison.OrdinalIgnoreCase))
        {
            if (response.Content.Headers.ContentType?.MediaType == null) return null;

            var queryParameters = HttpUtility.ParseQueryString(new Uri(url).Query);
            var name = queryParameters["filename"] ?? string.Empty;
            var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);

            return [new FileItem
            {
                Contents = BinaryData.FromBytes(bytes),
                MimeType = response.Content.Headers.ContentType.MediaType,
                Uri = url,
                Filename = name
            }];
        }

        return [await response.ToFileItem(url, cancellationToken: cancellationToken)];
    }

    public async Task<SimplicateNewItemData?> PostContentAsync<T>(
        IServiceProvider serviceProvider,
        string url,
        string jsonPayload,
        CancellationToken cancellationToken = default)
    {
        var tokenProvider = serviceProvider.GetService<HeaderProvider>();
        var (key, secret) = await TryGetKeySecretAsync(tokenProvider, cancellationToken);
        if (key is null || secret is null)
            return null;

        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Add("Authentication-Key", key);
        client.DefaultRequestHeaders.Add("Authentication-Secret", secret);

        using var content = new StringContent(jsonPayload, System.Text.Encoding.UTF8, "application/json");
        using var response = await client.PostAsync(url, content, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            // Optional: log or throw error
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"POST failed: {response.StatusCode} - {error}");
        }

        // Usually returns JSON, not a file
        var respContent = await response.Content.ReadAsStringAsync(cancellationToken);

        return JsonSerializer.Deserialize<SimplicateNewItemData>(respContent);
    }

    private async Task<(string Key, string Secret)?> GetCredentialsAsync(
        string oid,
        CancellationToken cancellationToken)
    {
        if (_secretClient == null)
        {
            return null;
        }

        var secret = await _secretClient.GetSecretAsync(oid,
                 cancellationToken: cancellationToken);
        var raw = secret.Value.Value;

        if (string.IsNullOrWhiteSpace(secret.Value.Properties.ContentType)
            || string.IsNullOrWhiteSpace(secret.Value.Value))
        {
            return null;
        }

        return (secret.Value.Properties.ContentType, secret.Value.Value);
    }


    private async Task<(string? Key, string? Secret)> TryGetKeySecretAsync(HeaderProvider? tokenProvider, CancellationToken cancellationToken)
    {
        if (tokenProvider?.Headers?.ContainsKey("Authentication-Key") == true &&
            tokenProvider.Headers.TryGetValue("Authentication-Secret", out string? value))
        {
            return (
                tokenProvider.Headers["Authentication-Key"].ToString(), value.ToString()
            );
        }

        if (string.IsNullOrEmpty(tokenProvider?.Bearer))
            return (null, null);

        var oid = tokenProvider.GetOidClaim();
        if (string.IsNullOrEmpty(oid))
            return (null, null);

        var credentials = await GetCredentialsAsync(oid, cancellationToken);
        return credentials is null
            ? (null, null)
            : (credentials.Value.Key, credentials.Value.Secret);
    }
}
