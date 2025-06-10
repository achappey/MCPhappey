using System.ComponentModel;
using MCPhappey.Tools.Extensions;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace MCPhappey.Tools.Kroki;

public static class KrokiDiagrams
{
    [Description("Generate a Kroki diagram from code and diagram type")]
    [McpServerTool(ReadOnly = false)]
    public static async Task<CallToolResponse> Kroki_CreateDiagram(
      [Description("Diagram type, e.g. graphviz, mermaid, plantuml, etc.")] string diagramType,
      [Description("The diagram source code (DOT, Mermaid, etc.)")] string diagramCode,
      [Description("Output file type/format, e.g. svg, png, pdf")] string fileType,
      IServiceProvider serviceProvider,
      RequestContext<CallToolRequestParams> requestContext,
      CancellationToken cancellationToken = default)
    {
        var httpClientFactory = serviceProvider.GetService<IHttpClientFactory>()
            ?? throw new InvalidOperationException("No IHttpClientFactory found in service provider");
        var httpClient = httpClientFactory.CreateClient();

        var url = $"https://kroki.io/{diagramType}/{fileType}";

        // Prepare the HTTP POST request
        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(diagramCode, System.Text.Encoding.UTF8, "text/plain")
        };

        using var response = await httpClient.SendAsync(request,
            HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        var error = await response.ToCallToolResponseOrErrorAsync(cancellationToken);
        if (error != null)
            return error;

        var fileBytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        var base64 = Convert.ToBase64String(fileBytes);

        // Detect content type (simple switch, expand as needed)
        string contentType = fileType.ToLower() switch
        {
            "svg" => "image/svg+xml",
            "png" => "image/png",
            "pdf" => "application/pdf",
            _ => "application/octet-stream"
        };

        List<Content> content = [new Content(){
                Type = "image",
                MimeType = contentType,
                Data = base64
            }];

        return new CallToolResponse()
        {
            Content = content
        };
    }

}