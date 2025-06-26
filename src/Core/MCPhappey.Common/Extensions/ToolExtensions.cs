using ModelContextProtocol.Protocol;

namespace MCPhappey.Common.Extensions;

public static class ToolExtensions
{
    public static CallToolResult ToErrorCallToolResponse(this string content)
         => new()
         {
             IsError = true,
             Content = [content.ToTextContentBlock()]
         };

    public static CallToolResult ToTextCallToolResponse(this string content)
         => new()
         {
             Content = [content.ToTextContentBlock()]
         };

    public static CallToolResult ToJsonCallToolResponse(this string content, string uri)
         => new()
         {
             Content = [content.ToJsonContent(uri)]
         };

    public static TextContentBlock ToTextContentBlock(this string contents) => new()
    {
        Text = contents
    };

    public static CallToolResult ToCallToolResult(this ReadResourceResult result) => new()
    {
        Content = [.. result.Contents.Select(z => z.ToContent())],
    };

    public static CallToolResult ToCallToolResult(this ContentBlock result) => new()
    {
        Content = [result],
    };

    public static CallToolResult ToCallToolResult(this IEnumerable<ContentBlock> results) => new()
    {
        Content = [.. results],
    };

}
