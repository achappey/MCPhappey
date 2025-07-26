using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using MCPhappey.Common.Extensions;
using MCPhappey.Core.Extensions;
using Microsoft.Graph.Beta;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace MCPhappey.Tools.HTML;

public static partial class HTMLPlugin
{
    private static async Task<IEnumerable<string>?> GetArguments(
      string sourceUrl,
      GraphServiceClient client,
      CancellationToken cancellationToken = default)
    {
        var driveItem = await client.GetDriveItem(sourceUrl, cancellationToken);
        var contentStream = await client
                .Drives[driveItem?.ParentReference?.DriveId]
                .Items[driveItem?.Id]
                .Content
                .GetAsync(cancellationToken: cancellationToken) ?? throw new Exception();

        string html;
        using (var reader = new StreamReader(contentStream))
            html = await reader.ReadToEndAsync(cancellationToken);

        // Find all {argument} tags with regex
        var matches = HtmlArgumentsRegex().Matches(html);
        return [.. matches
            .Select(m => m.Groups[1].Value)
            .Distinct()];
    }

    [Description("Reads an HTML file from OneDrive and returns all unique argument placeholders (like {name}) found in the file.")]
    [McpServerTool(Name = "HTMLPlugin_ListTemplateArguments",
        Title = "List template arguments in an HTML file",
        OpenWorld = false, ReadOnly = true)]
    public static async Task<CallToolResult?> HTMLPlugin_ListTemplateArguments(
      [Description("Url to the source HTML file")] string sourceUrl,
      IServiceProvider serviceProvider,
      RequestContext<CallToolRequestParams> requestContext,
      CancellationToken cancellationToken = default)
    {
        var client = await serviceProvider.GetOboGraphClient(requestContext.Server);
        var arguments = await GetArguments(sourceUrl, client, cancellationToken);

        return arguments.ToJsonContentBlock(sourceUrl).ToCallToolResult();
    }

    [Description("Reads an HTML file from OneDrive, replaces {argument} tags with provided values, and uploads the result as a new HTML file.")]
    [McpServerTool(Name = "HTMLPlugin_FillTemplate",
        Title = "Fill an HTML template and upload the result",
        OpenWorld = false)]
    public static async Task<CallToolResult?> HTMLPlugin_FillTemplate(
        [Description("Url to the source HTML file")] string sourceUrl,
        [Description("Name of the new HTML file that should be created (without extension)")] string newFilename,
        IServiceProvider serviceProvider,
        RequestContext<CallToolRequestParams> requestContext,
        [Description("Default values for the replacements. Format: key is argument name (without braces), value is replacement.")] Dictionary<string, string>? replacements = null,
        CancellationToken cancellationToken = default)
    {
        var client = await serviceProvider.GetOboGraphClient(requestContext.Server);
        var arguments = await GetArguments(sourceUrl, client, cancellationToken);
        var values = await requestContext.Server.ElicitAsync(
            new ElicitRequestParams
            {
                Message = "Please fill in the values of the HTML template".ToElicitDefaultData(replacements),
                RequestedSchema = new ElicitRequestParams.RequestSchema
                {
                    Properties = arguments?.ToDictionary(
                        a => a,
                        a => (ElicitRequestParams.PrimitiveSchemaDefinition)new ElicitRequestParams.StringSchema
                        {
                            Title = a,
                        }
                    ) ?? [],
                    Required = arguments?.ToList()
                }
            }, cancellationToken);

        var (typed, notAccepted) = await requestContext.Server.TryElicit(
              new HtmlNewFile { Name = newFilename },
              cancellationToken);

        if (notAccepted != null) return notAccepted;

        var driveItem = await client.GetDriveItem(sourceUrl, cancellationToken);
        var contentStream = await client
                .Drives[driveItem?.ParentReference?.DriveId]
                .Items[driveItem?.Id]
                .Content
                .GetAsync(cancellationToken: cancellationToken) ?? throw new Exception();

        string html;
        using (var reader = new StreamReader(contentStream))
            html = await reader.ReadToEndAsync(cancellationToken);

        // 2. Replace all {argument} tags
        foreach (var pair in values?.Content?.ToList() ?? [])
            html = html.Replace($"{{{pair.Key}}}", pair.Value.ToString() ?? string.Empty);

        // 4. Upload as new HTML file in root of the drive
        var outputName = $"{typed?.Name}.html";
        var uploadStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(html));

        var myDrive = await client.Me.Drive.GetAsync(cancellationToken: cancellationToken);
        var uploadedItem = await client.Drives[myDrive?.Id].Root.ItemWithPath($"/{outputName}")
            .Content.PutAsync(uploadStream, cancellationToken: cancellationToken);

        // 5. Return content block with download URL
        var url = $"https://graph.microsoft.com/beta/drives/{myDrive?.Id}/root:/{outputName}:/content";
        return uploadedItem.ToJsonContentBlock(url).ToCallToolResult();
    }


    [Description("Please fill in the new HTML file details.")]
    public class HtmlNewFile
    {
        [JsonPropertyName("name")]
        [Required]
        [Description("The name of the new file.")]
        public string Name { get; set; } = default!;
    }

    [System.Text.RegularExpressions.GeneratedRegex(@"\{([a-zA-Z0-9_]+)\}")]
    private static partial System.Text.RegularExpressions.Regex HtmlArgumentsRegex();
}