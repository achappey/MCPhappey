
using System.Net.Mime;
using MCPhappey.Common.Models;
using ModelContextProtocol.Protocol;

namespace MCPhappey.Common.Extensions;

public static class ModelContextProtocolExtensions
{
    public static bool ShouldLog(this LoggingLevel messageLevel, LoggingLevel? minLevel)
        => messageLevel >= (minLevel ?? LoggingLevel.Info);


    public static CallToolResult ToErrorCallToolResponse(this string content)
         => new()
         {
             IsError = true,
             Content = [content.ToTextContentBlock()]
         };

    public static CallToolResult ToTextCallToolResponse(this string content)
         => new()
         {
             Content = [content.ToTextContentBlock()]
         };

    public static CallToolResult ToJsonCallToolResponse(this string content, string uri)
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


    public static TextContentBlock ToTextContentBlock(this string contents) => new()
    {
        Text = contents
    };

    public static EmbeddedResourceBlock ToTextResourceContent(this string contents, string uri,
        string mimeType = MediaTypeNames.Text.Plain) => new()
        {
            Resource = new TextResourceContents()
            {
                Uri = uri,
                Text = contents,
                MimeType = mimeType
            }
        };

    public static ContentBlock ToJsonContent(this string contents, string uri) =>
        contents.ToTextResourceContent(uri, MediaTypeNames.Application.Json);


    private static EmbeddedResourceBlock ToContent(this ResourceContents contents) => new EmbeddedResourceBlock()
    {
        Resource = contents
    };

    public static CallToolResult ToCallToolResult(this ReadResourceResult result) => new()
    {
        Content = [.. result.Contents.Select(z => z.ToContent())],
    };

    public static CallToolResult ToCallToolResult(this ContentBlock result) => new()
    {
        Content = [result],
    };

     public static CallToolResult ToCallToolResult(this IEnumerable<ContentBlock> results) => new()
    {
        Content = [.. results],
    };


    public static string? ToText(this CreateMessageResult result) =>
         result.Content is TextContentBlock textContentBlock ? textContentBlock.Text : null;

    public static ModelPreferences? ToModelPreferences(this string? result) => result != null ? new()
    {
        Hints = [result.ToModelHint()!]
    } : null;

    public static ModelHint? ToModelHint(this string? result) => new() { Name = result };

}
