
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
        var supportedDecoders = contentDecoders
            .Where(a => a.SupportsMimeType(contentType));

        FileContent? fileContent = null;

        foreach (var decoder in supportedDecoders)
        {
            
            fileContent = await decoder.DecodeAsync(binaryData, cancellationToken);
        }

        return fileContent != null ? fileContent.GetFileItemFromFileContent(uri) : new FileItem()
        {
            Contents = binaryData,
            MimeType = contentType,
            Uri = uri
        };
    }
}
