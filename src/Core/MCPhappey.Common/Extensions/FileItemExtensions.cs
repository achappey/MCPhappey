using MCPhappey.Common.Models;
using System.Net.Mime;

namespace MCPhappey.Common.Extensions;

public static class FileItemExtensions
{
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


}

