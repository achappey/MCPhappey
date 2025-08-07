using MCPhappey.Common.Models;
using ModelContextProtocol.Protocol;
using System.Net.Mime;

namespace MCPhappey.Common.Extensions;

public static class FileItemExtensions
{
    public static FileItem ToFileItem<T>(this T content, string uri, string? filename = null) => new()
    {
        Contents = BinaryData.FromObjectAsJson(content),
        MimeType = MediaTypeNames.Application.Json,
        Uri = uri,
        Filename = filename
    };

    public static FileItem ToFileItem(this BinaryData binaryData,
          string uri,
          string mimeType = MediaTypeNames.Text.Plain,
          string? filename = null)
              => new()
              {
                  Contents = binaryData,
                  MimeType = mimeType,
                  Filename = filename,
                  Uri = uri,
              };


    public static FileItem ToJsonFileItem(this string content, string uri)
      => content.ToFileItem(uri, MediaTypeNames.Application.Json);

    public static FileItem ToFileItem(this string content,
        string uri,
        string mimeType = MediaTypeNames.Text.Plain)
            => new()
            {
                Contents = BinaryData.FromString(content),
                MimeType = mimeType,
                Uri = uri,
            };

    public static async Task<FileItem> ToFileItem(this HttpResponseMessage httpResponseMessage, string uri,
        CancellationToken cancellationToken = default) => new()
        {
            Contents = BinaryData.FromBytes(await httpResponseMessage.Content.ReadAsByteArrayAsync(cancellationToken)),
            MimeType = httpResponseMessage.Content.Headers.ContentType?.MediaType!,
            Uri = uri,
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

    public static IEnumerable<ContentBlock> ToContentBlocks(this IEnumerable<FileItem> fileItems)
              => fileItems.Select(a => new EmbeddedResourceBlock()
              {
                  Resource = a.ToResourceContents()
              });

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

}

