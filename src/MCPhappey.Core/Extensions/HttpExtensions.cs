
using MCPhappey.Core.Models;

namespace MCPhappey.Core.Extensions;

public static class HttpExtensions
{

    public static async Task<FileItem> ToFileItem(this HttpResponseMessage httpResponseMessage, string uri,
     CancellationToken cancellationToken = default) => new()
     {
         Contents = BinaryData.FromBytes(await httpResponseMessage.Content.ReadAsByteArrayAsync(cancellationToken)),
         MimeType = httpResponseMessage.Content.Headers.ContentType?.MediaType!,
         Uri = uri,
     };

}