using System.Net.Mime;
using MCPhappey.Common.Models;

namespace MCPhappey.Tools.KernelMemory;

public static class KernelMemoryExtensions
{
    public static FileItem GetFileItemFromFileContent(this Microsoft.KernelMemory.DataFormats.FileContent file, string uri)
        => new()
        {
            Contents = BinaryData.FromString(string.Join("\\n\\n",
                file.Sections.Select(a => a.Content))),
            MimeType = MediaTypeNames.Text.Plain,
            Uri = uri,
        };

}
