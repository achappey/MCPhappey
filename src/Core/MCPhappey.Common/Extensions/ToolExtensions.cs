using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

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
    public static CallToolResult ToResourceLinkCallToolResponse(this ResourceLinkBlock resourceLinkBlock)
         => new()
         {
             Content = [resourceLinkBlock]
         };

    public static CallToolResult ToResourceLinkCallToolResponse(this IEnumerable<ResourceLinkBlock> resourceLinkBlocks)
            => new()
            {
                Content = [.. resourceLinkBlocks]
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

    public static ResourceLinkBlock ToResourceLinkBlock(this string uri, string name, string? mimeType = null, string? description = null, long? size = null) => new()
    {
        Uri = uri,
        Name = name,
        Description = description,
        MimeType = mimeType,
        Size = size
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

    public static string ToOutputFileName(this RequestContext<CallToolRequestParams> context, string extension)
      => $"{context.Params?.Name ?? context.Server.ServerOptions.ServerInfo?.Name}_{DateTime.Now.Ticks}.{extension.ToLower()}";


}
