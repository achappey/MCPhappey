
using MCPhappey.Common.Models;
using MCPhappey.Core.Extensions;
using Microsoft.KernelMemory.DataFormats;

namespace MCPhappey.Core.Services;

public class TransformService(
    IEnumerable<IContentDecoder> contentDecoders)
{
    public async Task<FileItem> DecodeAsync(string uri, BinaryData binaryData, string contentType,
         CancellationToken cancellationToken = default)
    {
        string? myAssemblyName = typeof(TransformService).Namespace?.Split(".").FirstOrDefault();

        var bestDecoder = contentDecoders
            .Where(a => a.SupportsMimeType(contentType))
            .OrderBy(d => myAssemblyName != null
                && d.GetType().Namespace?.Contains(myAssemblyName) == true ? 0 : 1)
            .FirstOrDefault();

        FileContent? fileContent = null;
        if (bestDecoder != null)
        {
            fileContent = await bestDecoder.DecodeAsync(binaryData, cancellationToken);
        }

        // Fallback: original content if nothing could decode
        return fileContent != null
            ? fileContent.GetFileItemFromFileContent(uri)
            : new FileItem
            {
                Contents = binaryData,
                MimeType = contentType,
                Uri = uri
            };
    }
}
