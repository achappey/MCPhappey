
using System.Net.Mime;
using MCPhappey.Common.Models;
using ModelContextProtocol.Protocol;

namespace MCPhappey.Common.Extensions;

public static class ModelContextProtocolExtensions
{
    public static bool ShouldLog(this LoggingLevel messageLevel, LoggingLevel? minLevel)
        => messageLevel >= (minLevel ?? LoggingLevel.Info);


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

    public static ReadResourceResult ToReadResourceResult(this IEnumerable<FileItem> fileItems)
          => new()
          {
              Contents = [.. fileItems.Select(a => a.ToResourceContents())]
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
        Type = "resource",
        Resource = contents
    };

    public static CallToolResponse ToCallToolResponse(this ReadResourceResult result) => new()
    {
        Content = [.. result.Contents.Select(z => z.ToContent())],
    };

    public static ModelPreferences? ToModelPreferences(this string? result) => result != null ? new()
    {
        Hints = [new ModelHint() { Name = result }]
    } : null;
}
