
using Microsoft.KernelMemory.DataFormats.Office;
using Microsoft.KernelMemory.DataFormats.Pdf;
using MCPhappey.Core.Extensions;
using MCPhappey.Core.Models;
using System.Net.Mime;

namespace MCPhappey.Core.Services;

public class TransformService(MsExcelDecoder msExcelDecoder,
    MsWordDecoder msWordDecoder,
    MsPowerPointDecoder msPowerPointDecoder,
    PdfDecoder pdfDecoder)
{
    public async Task<FileItem> TransformContentAsync(string uri, BinaryData binaryData, string contentType,
          CancellationToken cancellationToken = default)
    {
        return contentType switch
        {
            "application/epub+zip" => await binaryData.GetFileItemFromEbook(uri),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
                => (await msExcelDecoder.DecodeAsync(binaryData, cancellationToken)).GetFileItemFromFileContent(uri),
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
                => (await msWordDecoder.DecodeAsync(binaryData, cancellationToken)).GetFileItemFromFileContent(uri),
            "application/vnd.openxmlformats-officedocument.presentationml.presentation"
                => (await msPowerPointDecoder.DecodeAsync(binaryData, cancellationToken)).GetFileItemFromFileContent(uri),
            MediaTypeNames.Application.Pdf
                => (await pdfDecoder.DecodeAsync(binaryData, cancellationToken)).GetFileItemFromFileContent(uri),
            MediaTypeNames.Text.Html => binaryData.GetFileItemFromHtml(uri),
            MediaTypeNames.Text.Plain
            or ""
            or "application/hal+json"
            or MediaTypeNames.Text.Csv
            or MediaTypeNames.Application.Json
            or MediaTypeNames.Application.ProblemJson
            or MediaTypeNames.Text.Markdown
            or MediaTypeNames.Application.Xml => new()
            {
                Contents = binaryData,
                MimeType = contentType,
                Uri = uri,
            },
            _ => throw new NotSupportedException()
        };
    }
}
