using MCPhappey.Core.Models;
using HtmlAgilityPack;
using System.Net.Mime;
using Microsoft.KernelMemory.DataFormats.WebPages;
using VersOne.Epub;
using System.Text;

namespace MCPhappey.Core.Extensions;

public static class FileExtensions
{
    public static FileItem GetFileItemFromHtml(this BinaryData binaryData, string uri)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(binaryData.ToString());

        // Remove <script> and <style> nodes
        foreach (var node in doc.DocumentNode.SelectNodes("//script|//style") ?? Enumerable.Empty<HtmlNode>())
        {
            node.Remove();
        }

        // Extract visible text
        return new FileItem()
        {
            Contents = BinaryData.FromString(string.Join("\n", doc.DocumentNode.Descendants()
                            .Where(n => n.NodeType == HtmlNodeType.Text && !string.IsNullOrWhiteSpace(n.InnerText))
                            .Select(n => n.InnerText.Trim()))),
            MimeType = MediaTypeNames.Text.Plain,
            Uri = uri,
        };
    }

    private static string PrintTextContentFile(this EpubLocalTextContentFile textContentFile)
    {
        HtmlDocument htmlDocument = new();
        htmlDocument.LoadHtml(textContentFile.Content);
        StringBuilder sb = new();

        foreach (HtmlNode node in htmlDocument.DocumentNode.SelectNodes("//text()"))
        {
            sb.AppendLine(node.InnerText.Trim());
        }

        return sb.ToString();
    }

    public static async Task<FileItem> GetFileItemFromEbook(this BinaryData binaryData, string uri)
    {
        var book = await EpubReader.ReadBookAsync(binaryData.ToStream());

        StringBuilder sb = new();

        foreach (EpubLocalTextContentFile textContentFile in book.ReadingOrder)
        {
            sb.AppendLine(textContentFile.PrintTextContentFile());
        }

        return new FileItem()
        {
            Contents = BinaryData.FromString(sb.ToString()),
            MimeType = MediaTypeNames.Text.Plain,
            Uri = uri,
        };
    }

    public static async Task<FileItem> ToFileItem(this HttpResponseMessage httpResponseMessage, string uri,
       CancellationToken cancellationToken = default) => new()
       {
           Contents = BinaryData.FromBytes(await httpResponseMessage.Content.ReadAsByteArrayAsync(cancellationToken)),
           MimeType = httpResponseMessage.Content.Headers.ContentType?.MediaType!,
           Uri = uri,
       };

    public static FileItem ToFileItem(this WebScraperResult webScraperResult, string uri) => new()
    {
        Contents = webScraperResult.Content,
        MimeType = webScraperResult.ContentType,
        Uri = uri,
    };
}