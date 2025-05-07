
using System.Net.Mime;
using MCPhappey.Core.Models;
using MCPhappey.Core.Models.Protocol;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using ModelContextProtocol.Protocol.Types;

namespace MCPhappey.Core.Extensions;

public static class ModelContextProtocolExtensions
{
    public static CallToolResponse ToErrorCallToolResponse(this string content)
         => new()
         {
             IsError = true,
             Content = [content.ToTextContent()]
         };

    public static CallToolResponse ToTextCallToolResponse(this string content)
         => new()
         {
             Content = [content.ToTextContent()]
         };

    public static CallToolResponse ToJsonCallToolResponse(this string content, string uri)
         => new()
         {
             Content = [content.ToJsonContent(uri)]
         };

    public static ReadResourceResult ToReadResourceResult(this string content,
        string uri,
        string mimeType = MediaTypeNames.Text.Plain)
        => new()
        {
            Contents =
                [
                    new TextResourceContents()
                    {
                        Text = content,
                        MimeType = mimeType,
                        Uri = uri,
                    }
                ]
        };

    public static ReadResourceResult ToReadResourceResult(this FileItem fileItem)
          => new()
          {
              Contents =
                  [
                    fileItem.ToResourceContents()
                  ]
          };

    public static ResourceContents ToResourceContents(this FileItem fileItem)
        => fileItem.MimeType.StartsWith("text/")
            || fileItem.MimeType.Equals(MediaTypeNames.Application.Json)
            || fileItem.MimeType.Equals(MediaTypeNames.Application.ProblemJson)
            || fileItem.MimeType.Equals("application/hal+json")            
            || fileItem.MimeType.Equals(MediaTypeNames.Application.Xml) ? new TextResourceContents()
            {
                Text = fileItem.Contents.ToString(),
                MimeType = fileItem.MimeType,
                Uri = fileItem.Uri,
            } : new BlobResourceContents()
            {
                Blob = Convert.ToBase64String(fileItem.Contents),
                MimeType = fileItem.MimeType,
                Uri = fileItem.Uri,
            };

    public static ReadResourceResult ToJsonReadResourceResult(this string content, string uri)
        => content.ToReadResourceResult(uri, MediaTypeNames.Application.Json);


    public static Content ToTextContent(this string contents) => new()
    {
        Text = contents,
        Type = "text"
    };

    public static Content ToTextResourceContent(this string contents, string uri,
        string mimeType = MediaTypeNames.Text.Plain) => new()
        {
            Type = "resource",
            Resource = new TextResourceContents()
            {
                Uri = uri,
                Text = contents,
                MimeType = mimeType
            }
        };

    public static Content ToJsonContent(this string contents, string uri) =>
        contents.ToTextResourceContent(uri, MediaTypeNames.Application.Json);

    private static Content ToContent(this ResourceContents contents) => new()
    {
        Resource = contents
    };

    public static CallToolResponse ToCallToolResponse(this ReadResourceResult result) => new()
    {
        Content = [.. result.Contents.Select(z => z.ToContent())]
    };

    public static ModelPreferences? ToModelPreferences(this string? result) => result != null ? new()
    {
        Hints = [new ModelHint() { Name = result }]
    } : null;

    public static IMcpServerBuilder WithConfigureSessionOptions(this IMcpServerBuilder mcpBuilder,
        IEnumerable<ServerConfig> servers) => mcpBuilder.WithHttpTransport(http =>
        {
            http.ConfigureSessionOptions = async (ctx, opts, cancellationToken) =>
            {
                await Task.Run(() =>
                {
                    var kernel = ctx.RequestServices.GetRequiredService<Kernel>();

                    var serverName = ctx.Request.Path.Value!.GetServerNameFromUrl();
                    var server = servers.First(a => a.Server.ServerInfo?.Name.Equals(serverName, StringComparison.OrdinalIgnoreCase) == true);

                    opts.ServerInfo = server.Server.ToServerInfo();
                    opts.Capabilities = new()
                    {
                        Resources = server.Server.ToResourcesCapability(ctx),
                        Prompts = server.Server.ToPromptsCapability(ctx),
                        Tools = server.Server.ToToolsCapability(kernel)
                    };
                }, cancellationToken);
            };
        });
}
