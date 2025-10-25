using MCPhappey.Common.Models;
using ModelContextProtocol.Protocol;
using System.Net.Mime;
using System.Text;
using System.Text.Json;

namespace MCPhappey.Common.Extensions;

public static class FileItemExtensions
{
    public static async Task<string> ToStringValueAsync(this Stream stream) => (await BinaryData.FromStreamAsync(stream)).ToString();
    public static string ToStringValue(this Stream stream) => BinaryData.FromStream(stream).ToString();

    public static byte[] ToArray(this Stream stream) => BinaryData.FromStream(stream).ToArray();

    public static FileItem ToFileItem<T>(this T content, string uri, string? filename = null) => new()
    {
        Contents = BinaryData.FromObjectAsJson(content, JsonSerializerOptions.Web),
        //Stream = BinaryData.FromObjectAsJson(content, JsonSerializerOptions.Web).ToStream(),
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
                  //Stream = binaryData.ToStream(),
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
                //  Stream = BinaryData.FromString(content).ToStream(),
                Contents = BinaryData.FromString(content),
                MimeType = mimeType,
                Uri = uri,
            };

    public static async Task<FileItem> ToFileItem(this HttpResponseMessage httpResponseMessage, string uri,
        CancellationToken cancellationToken = default) => new()
        {
            Contents = BinaryData.FromBytes(await httpResponseMessage.Content.ReadAsByteArrayAsync(cancellationToken)),
            // Stream = await httpResponseMessage.Content.ReadAsStreamAsync(),
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
            || (fileItem.MimeType.StartsWith("application/") && fileItem.MimeType.EndsWith("+json"))
            || (fileItem.MimeType.StartsWith("application/", StringComparison.OrdinalIgnoreCase) && fileItem.MimeType.EndsWith("+xml"))
            || fileItem.MimeType.Equals(MediaTypeNames.Application.Xml) ? new TextResourceContents()
            {
                //Text = Encoding.UTF8.GetString(BinaryData.FromStream(fileItem.Stream!)),
                Text = Encoding.UTF8.GetString(fileItem.Contents.ToArray()),
                //Text = fileItem.Stream != null ? Encoding.UTF8.GetString(BinaryData.FromStream(fileItem.Stream))
                //   : Encoding.UTF8.GetString(fileItem.Contents.ToArray()),
                MimeType = fileItem.MimeType,
                Uri = fileItem.Uri,
            } : new BlobResourceContents()
            {
                //   Blob = Convert.ToBase64String(BinaryData.FromStream(fileItem.Stream!)),
                Blob = Convert.ToBase64String(fileItem.Contents.ToArray()),
                //  Blob = fileItem.Stream != null ? Convert.ToBase64String(BinaryData.FromStream(fileItem.Stream))
                //       : Convert.ToBase64String(fileItem.Contents.ToArray()),
                MimeType = fileItem.MimeType,
                Uri = fileItem.Uri,
            };

}

