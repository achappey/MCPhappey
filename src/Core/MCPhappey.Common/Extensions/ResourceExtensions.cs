using System.Net.Mime;
using System.Text.Json;
using ModelContextProtocol.Protocol;

namespace MCPhappey.Common.Extensions;

public static class ResourceExtensions
{
    public static EmbeddedResourceBlock ToJsonContentBlock<T>(this T content, string uri)
            => new()
            {
                Resource = new TextResourceContents()
                {
                    MimeType = "application/json",
                    Text = JsonSerializer.Serialize(content, JsonSerializerOptions.Web),
                    Uri = uri
                }
            };

    public static ReadResourceResult ToReadResourceResult(this string content,
        string uri,
        string mimeType = MediaTypeNames.Text.Plain)
        => new()
        {
            Contents =
                [
                    new TextResourceContents()
                    {
                        Text = content,
                        MimeType = mimeType,
                        Uri = uri,
                    }
                ]
        };


    public static ReadResourceResult ToJsonReadResourceResult(this string content, string uri)
        => content.ToReadResourceResult(uri, MediaTypeNames.Application.Json);

    public static EmbeddedResourceBlock ToTextResourceContent(this string contents, string uri,
        string mimeType = MediaTypeNames.Text.Plain) => new()
        {
            Resource = new TextResourceContents()
            {
                Uri = uri,
                Text = contents,
                MimeType = mimeType
            }
        };

    public static ContentBlock ToJsonContent(this string contents, string uri) =>
        contents.ToTextResourceContent(uri, MediaTypeNames.Application.Json);


    public static EmbeddedResourceBlock ToContent(this ResourceContents contents) => new()
    {
        Resource = contents
    };
}
