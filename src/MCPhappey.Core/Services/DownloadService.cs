
using System.Text.RegularExpressions;
using MCPhappey.Core.Extensions;
using MCPhappey.Core.Models;
using MCPhappey.Common.Models;
using Microsoft.KernelMemory.DataFormats.WebPages;
using MCPhappey.Auth.Models;
using MCPhappey.Auth.Extensions;

namespace MCPhappey.Core.Services;

public partial class DownloadService(WebScraper webScraper,
    TransformService transformService,
    IHttpClientFactory httpClientFactory,
    OAuthSettings oAuthSettings,
    Dictionary<string, Dictionary<string, string>>? domainHeaders = null) : IWebScraper
{
    public async Task<WebScraperResult> GetContentAsync(string url, CancellationToken cancellationToken = default)
    {
        return await webScraper.GetContentAsync(url, cancellationToken);
    }

    public async Task<FileItem> GetContentAsync(ServerConfig serverConfig, string url,
           string? authToken = null, CancellationToken cancellationToken = default)
    {
        Uri uri = new(url);
        FileItem? resultFile = null;

        if (domainHeaders != null
            && domainHeaders.TryGetValue(uri.Host, out Dictionary<string, string>? value))
        {
            // Direct API mapping from appsettings
            var httpClient = httpClientFactory.CreateClient();

            foreach (var item in value)
            {
                httpClient.DefaultRequestHeaders.Add(item.Key, [item.Value]);
            }

            using var result = await httpClient.GetAsync(url, cancellationToken);
            resultFile = await result.ToFileItem(url, cancellationToken: cancellationToken);
        }
        else
        {
            //var serverConfig = servers.GetServerConfig(httpContextAccessor.HttpContext);

            if (serverConfig.Server.Metadata?.ContainsKey(uri.Host) == true)
            {
                var httpClient = await httpClientFactory.GetOboHttpClient(authToken!, uri.Host, serverConfig.Server, oAuthSettings);
                using var result = await httpClient.GetAsync(url, cancellationToken);
                resultFile = await result.ToFileItem(url, cancellationToken: cancellationToken);
            }
            else if (uri.Host.EndsWith(".sharepoint.com"))
            {
                // Graph API mapping
                using var graphClient = await httpClientFactory.GetOboGraphClient(authToken!,
                    serverConfig.Server, oAuthSettings);

                resultFile = await graphClient.GetFilesByUrl(url);
            }
            else
            {
                // No API mapping
                var defaultScraper = await webScraper.GetContentAsync(url, cancellationToken);
                resultFile = defaultScraper.ToFileItem(url);
            }
        }

        return await transformService.DecodeAsync(url, resultFile.Contents,
               resultFile.MimeType, cancellationToken);
    }

    [GeneratedRegex(@"^[^.]+\.crm\d+\.dynamics\.com$", RegexOptions.IgnoreCase | RegexOptions.Compiled, "nl-NL")]
    private static partial Regex DynamicsHostPattern();

    [GeneratedRegex(@"^[^.]+\.simplicate\.app$", RegexOptions.IgnoreCase | RegexOptions.Compiled, "nl-NL")]
    private static partial Regex SimplicateHostPattern();


}
