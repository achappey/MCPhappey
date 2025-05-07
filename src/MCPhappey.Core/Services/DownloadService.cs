
using System.Text.RegularExpressions;
using MCPhappey.Core.Extensions;
using MCPhappey.Core.Models;
using MCPhappey.Core.Models.Protocol;
using Microsoft.AspNetCore.Http;
using Microsoft.KernelMemory.DataFormats.WebPages;

namespace MCPhappey.Core.Services;

public partial class DownloadService(WebScraper webScraper,
    IHttpContextAccessor httpContextAccessor,
    IReadOnlyList<ServerConfig> servers,
    TransformService transformService,
    IHttpClientFactory httpClientFactory,
    Dictionary<string, Dictionary<string, string>>? domainHeaders = null) : IWebScraper
{
    public async Task<FileItem> GetContentAsync(string url,
        HttpContext httpContext, CancellationToken cancellationToken = default)
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
            var serverName = httpContext.Request.Path.Value!.GetServerNameFromUrl();
            var serverConfig = servers.FirstOrDefault(a => a.Server.ServerInfo.Name.Equals(serverName, StringComparison.OrdinalIgnoreCase));

            if (serverConfig?.Auth?.OBO?.ContainsKey(uri.Host) == true)
            {
                // OBO API mapping
                var httpClient = await httpClientFactory.GetOboHttpClient(httpContext.GetBearerToken()!, uri.Host, serverConfig.Auth);
                using var result = await httpClient.GetAsync(url, cancellationToken);
                resultFile = await result.ToFileItem(url, cancellationToken: cancellationToken);
            }
            else if (uri.Host.EndsWith(".sharepoint.com"))
            {
                // Graph API mapping
                using var graphClient = await httpClientFactory.GetOboGraphClient(httpContext.GetBearerToken()!,
                    serverConfig?.Auth);

                resultFile = await graphClient.GetFilesByUrl(url);
            }
            else
            {
                // No API mapping
                var defaultScraper = await webScraper.GetContentAsync(url, cancellationToken);
                resultFile = defaultScraper.ToFileItem(url);
            }
        }

        return await transformService.TransformContentAsync(url, resultFile.Contents,
               resultFile.MimeType, cancellationToken);
    }

    public async Task<WebScraperResult> GetContentAsync(string url, CancellationToken cancellationToken = default)
    {
        try
        {
            var context = httpContextAccessor.HttpContext;
            var download = await GetContentAsync(url, httpContextAccessor.HttpContext!, cancellationToken);

            return download != null ? new WebScraperResult()
            {
                Content = download.Contents,
                ContentType = download.MimeType,
                Success = true,
            } : new WebScraperResult
            {
                Success = false,
                Error = "Something went wrong"
            };

        }
        catch (Exception e)
        {
            return new WebScraperResult
            {
                Success = false,
                Error = e.Message
            };
        }
    }


    [GeneratedRegex(@"^[^.]+\.crm\d+\.dynamics\.com$", RegexOptions.IgnoreCase | RegexOptions.Compiled, "nl-NL")]
    private static partial Regex DynamicsHostPattern();

    [GeneratedRegex(@"^[^.]+\.simplicate\.app$", RegexOptions.IgnoreCase | RegexOptions.Compiled, "nl-NL")]
    private static partial Regex SimplicateHostPattern();

}
