using System.ComponentModel;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using MCPhappey.Core.Extensions;
using MCPhappey.Core.Services;
using MCPhappey.Tools.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.KernelMemory.Pipeline;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace MCPhappey.Tools.Cohere;

public static class CohereService
{
    [Description("Rerank documents in a SharePoint or OneDrive folder using Cohere's rerank model.")]
    [McpServerTool(Title = "Cohere Rerank SharePoint Folder", Name = "cohere_rerank_sharepoint_folder", ReadOnly = true)]
    public static async Task<CallToolResult?> Cohere_RerankSharePointFolder(
       IServiceProvider serviceProvider,
       RequestContext<CallToolRequestParams> requestContext,
       [Description("Rerank model, e.g. rerank-v3.5")] string model,
       [Description("Input query to rank on")] string query,
       [Description("SharePoint or OneDrive folder URL")] string sharepointFolderUrl,
       [Description("The number of top results to return.")] int topN,
       CancellationToken cancellationToken = default)
       => await requestContext.WithExceptionCheck(async () =>
           await requestContext.WithOboGraphClient(async (graphClient) =>
           await requestContext.WithStructuredContent(async () =>
           {
               var result = await graphClient.GetDriveItem(sharepointFolderUrl);
               var fileUrls = new List<string>();

               if (result?.Folder != null)
               {
                   var items = await graphClient.Drives[result.ParentReference!.DriveId!]
                       .Items[result.Id!].Children.GetAsync(cancellationToken: cancellationToken);

                   foreach (var item in items?.Value?.Where(a => !string.IsNullOrEmpty(a.WebUrl)) ?? [])
                       fileUrls.Add(item.WebUrl!);
               }

               return await CohereRerankDocumentsAsync(
                   serviceProvider, requestContext, model, query, fileUrls, topN, cancellationToken);
           })));


    [Description("Rerank arbitrary text-based documents using Cohere's rerank API.")]
    [McpServerTool(Title = "Cohere Rerank Files", Name = "cohere_rerank_files", ReadOnly = true)]
    public static async Task<CallToolResult?> Cohere_RerankFiles(
        IServiceProvider serviceProvider,
        RequestContext<CallToolRequestParams> requestContext,
        [Description("Rerank model, e.g. rerank-v3.5")] string model,
        [Description("Input query to rank on")] string query,
        [Description("List of file URLs to rerank")] List<string> fileUrls,
        [Description("The number of top results to return.")] int topN,
        CancellationToken cancellationToken = default)
        => await requestContext.WithExceptionCheck(async () =>
            await requestContext.WithStructuredContent(async () =>
                await CohereRerankDocumentsAsync(
                    serviceProvider, requestContext, model, query, fileUrls, topN, cancellationToken)));


    private static async Task<JsonNode?> CohereRerankDocumentsAsync(
        IServiceProvider serviceProvider,
        RequestContext<CallToolRequestParams> requestContext,
        string model,
        string query,
        List<string> fileUrls,
        int topN,
        CancellationToken cancellationToken)
    {
        var settings = serviceProvider.GetRequiredService<CohereSettings>();
        var clientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
        var downloadService = serviceProvider.GetRequiredService<DownloadService>();

        var documents = new List<string>();
        var semaphore = new SemaphoreSlim(3);

        var tasks = fileUrls
            .Where(a => !string.IsNullOrWhiteSpace(a))
            .Select(async url =>
            {
                await semaphore.WaitAsync(cancellationToken);
                try
                {
                    var files = await downloadService.ScrapeContentAsync(
                        serviceProvider,
                        requestContext.Server,
                        url,
                        cancellationToken);

                    foreach (var f in files
                        .Where(a => a.MimeType.StartsWith("text/") || a.MimeType.StartsWith(MimeTypes.Json)))
                        documents.Add(f.Contents.ToString());
                }
                finally
                {
                    semaphore.Release();
                }
            })
            .ToList();

        await Task.WhenAll(tasks);

        if (documents.Count == 0)
            throw new Exception("No valid text documents found.");

        var body = new
        {
            model,
            query,
            documents,
            top_n = topN
        };

        var jsonBody = JsonSerializer.Serialize(body);

        using var client = clientFactory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.cohere.com/v2/rerank")
        {
            Content = new StringContent(jsonBody, Encoding.UTF8, MimeTypes.Json)
        };

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(MimeTypes.Json));
        request.Headers.Add("X-Client-Name", "MCPhappey");

        using var resp = await client.SendAsync(request, cancellationToken);
        var txt = await resp.Content.ReadAsStringAsync(cancellationToken);

        if (!resp.IsSuccessStatusCode)
            throw new Exception($"{resp.StatusCode}: {txt}");

        return JsonNode.Parse(txt);
    }
}


public class CohereSettings
{
    public string ApiKey { get; set; } = default!;
}
