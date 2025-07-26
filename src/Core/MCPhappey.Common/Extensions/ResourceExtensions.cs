using System.Net.Mime;
using System.Text.Json;
using ModelContextProtocol.Protocol;

namespace MCPhappey.Common.Extensions;

public static class ResourceExtensions
{
    public static readonly JsonSerializerOptions JsonSerializerOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public static EmbeddedResourceBlock ToJsonContentBlock<T>(this T content, string uri)
            => JsonSerializer.Serialize(content, JsonSerializerOptions)
            .ToTextResourceContent(uri, MediaTypeNames.Application.Json);

    public static ReadResourceResult ToReadResourceResult(this string content,
        string uri,
        string mimeType = MediaTypeNames.Text.Plain)
        => new()
        {
            Contents =
                [
                    content.ToTextResourceContents(uri, mimeType)
                ]
        };

    public static ReadResourceResult ToJsonReadResourceResult(this string content, string uri)
        => content.ToReadResourceResult(uri, MediaTypeNames.Application.Json);

    public static TextResourceContents ToTextResourceContents(this string contents, string uri,
           string mimeType = MediaTypeNames.Text.Plain) => new()
           {
               Uri = uri,
               Text = contents,
               MimeType = mimeType
           };

    public static EmbeddedResourceBlock ToTextResourceContent(this string contents, string uri,
        string mimeType = MediaTypeNames.Text.Plain) => new()
        {
            Resource = contents.ToTextResourceContents(uri, mimeType)
        };

    public static ContentBlock ToJsonContent(this string contents, string uri) =>
        contents.ToTextResourceContent(uri, MediaTypeNames.Application.Json);

    public static EmbeddedResourceBlock ToContent(this ResourceContents contents) => new()
    {
        Resource = contents
    };
}
