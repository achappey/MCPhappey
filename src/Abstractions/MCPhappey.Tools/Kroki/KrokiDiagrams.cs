using System.ComponentModel;
using System.Text.Json;
using MCPhappey.Common.Extensions;
using MCPhappey.Tools.Extensions;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace MCPhappey.Tools.Kroki;

public static class KrokiDiagrams
{
    public static readonly HashSet<string> AllowedTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "blockdiag",
        "bpmn",
        "bytefield",
        "seqdiag",
        "actdiag",
        "nwdiag",
        "packetdiag",
        "rackdiag",
        "c4plantuml",
        "d2",
        "dbml",
        "ditaa",
        "erd",
        "excalidraw",
        "graphviz",
        "mermaid",
        "nomnoml",
        "pikchr",
        "plantuml",
        "structurizr",
        "svgbob",
        "symbolator",
        "tikz",
        "vega",
        "vegalite",
        "wavedrom",
        "wireviz"
    };

    [Description("Generate a Kroki diagram from code and diagram type")]
    [McpServerTool(ReadOnly = false, Title = "Create a diagram with Kroki")]
    public static async Task<CallToolResponse> Kroki_CreateDiagram(
      [Description("Diagram type, e.g. graphviz, mermaid, plantuml, etc.")] string diagramType,
      [Description("The diagram source code (DOT, Mermaid, etc.)")] string diagramCode,
      [Description("Output file type/format, e.g. svg, png, pdf")] string fileType,
      IServiceProvider serviceProvider,
      RequestContext<CallToolRequestParams> requestContext,
      CancellationToken cancellationToken = default)
    {
        if (!AllowedTypes.Contains(diagramType))
        {
            return $"Unsupported diagram type '{diagramType}'. Allowed types: {string.Join(", ", AllowedTypes)}".ToErrorCallToolResponse();
        }

        var httpClientFactory = serviceProvider.GetService<IHttpClientFactory>()
            ?? throw new InvalidOperationException("No IHttpClientFactory found in service provider");
        var httpClient = httpClientFactory.CreateClient();

        var url = $"https://kroki.io/{diagramType}/{fileType}";

        // Prepare the HTTP POST request
        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(diagramCode, System.Text.Encoding.UTF8, "text/plain")
        };

        if (LoggingLevel.Info.ShouldLog(requestContext.Server.LoggingLevel))
        {
            var domain = new Uri(url).Host; // e.g., "example.com"
            var markdown =
                $"<details><summary>POST <a href=\"{url}\" target=\"blank\">{domain}</a></summary>\n\n```\n{diagramCode}\n```\n</details>";

            await requestContext.Server.SendNotificationAsync("notifications/message", new LoggingMessageNotificationParams()
            {
                Level = LoggingLevel.Info,
                Data = JsonSerializer.SerializeToElement(markdown),
            }, cancellationToken: CancellationToken.None);
        }

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