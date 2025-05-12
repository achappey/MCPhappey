
using MCPhappey.Core.Extensions;
using MCPhappey.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.KernelMemory.DataFormats;

namespace MCPhappey.Core.Services;

public class TransformService(
    IServiceProvider serviceProvider)
{
    public async Task<FileItem> DecodeAsync(string uri, BinaryData binaryData, string contentType,
         CancellationToken cancellationToken = default)
    {
        var decoders = serviceProvider.GetServices<IContentDecoder>();
        FileContent? fileContent = null;

        foreach (var decoder in decoders)
        {
            if (decoder.SupportsMimeType(contentType))
            {
                fileContent = await decoder.DecodeAsync(binaryData, cancellationToken);
            }
        }

        return fileContent != null ? fileContent.GetFileItemFromFileContent(uri) : new FileItem()
        {
            Contents = binaryData,
            MimeType = contentType,
            Uri = uri
        };
    }
}
