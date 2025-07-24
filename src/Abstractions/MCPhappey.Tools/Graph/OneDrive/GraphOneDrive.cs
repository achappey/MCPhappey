using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;
using MCPhappey.Common.Extensions;
using MCPhappey.Core.Extensions;
using Microsoft.Graph.Beta;
using Microsoft.Graph.Beta.Models;
using Microsoft.Kiota.Abstractions;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace MCPhappey.Tools.Graph.OneDrive;

public static class GraphOneDrive
{
    [Description("Uploads a file to the specified OneDrive location.")]
    [McpServerTool(Name = "GraphOneDrive_UploadFile", Title = "Upload file to OneDrive", OpenWorld = false)]
    public static async Task<CallToolResult?> GraphOneDrive_UploadFile(
        [Description("The OneDrive Drive ID.")] string driveId,
        [Description("The file name (e.g. foo.txt).")] string filename,
        [Description("The folder path in OneDrive (e.g. docs).")] string path,
        [Description("The file contents as a string.")] string content,
        IServiceProvider serviceProvider,
        RequestContext<CallToolRequestParams> requestContext,
        CancellationToken cancellationToken = default)
    {
        var (typed, notAccepted) = await requestContext.Server.TryElicit(
              new GraphUploadFile
              {
                  Name = filename,
                  Path = path,
                  Content = content
              },
              cancellationToken);

        if (notAccepted != null) return notAccepted;

        var client = await serviceProvider.GetOboGraphClient(requestContext.Server);
        var result = await client.Drives[driveId]
                .Items["root"].ItemWithPath($"/{typed?.Path}/{typed?.Name}")
                .Content.PutAsync(BinaryData.FromString(typed?.Content ?? string.Empty).ToStream(),
                   cancellationToken: cancellationToken);

        return result.ToJsonContentBlock($"https://graph.microsoft.com/beta/drives/{driveId}/items/root:/{path}/{filename}:/content")
         .ToCallToolResult();
    }

    /// <summary>Create a folder (or Document Set) in OneDrive / SharePoint.</summary>
    /*    [Description("Create a folder in the specified OneDrive or SharePoint document library.")]
        [McpServerTool(Name = "GraphDrive_CreateFolder", OpenWorld = false)]
        public static async Task<CallToolResult?> GraphDrive_CreateFolder(
            [Description("The OneDrive or SharePoint Drive ID.")] string driveId,
            [Description("The name of the new folder.")] string name,
            IServiceProvider serviceProvider,
            RequestContext<CallToolRequestParams> requestContext,
            [Description("Folder path within the document library. Leave empty for root. Use slashes for subfolders, e.g. 'Invoices/2025'.")]
            string? parentPath = "",
            [Description("The ID of the content type (for Document Set, optional).")]
            string? contentTypeId = null,
            CancellationToken cancellationToken = default)
        {
            var client = await serviceProvider.GetOboGraphClient(requestContext.Server);

            // -- Ask user / AI to confirm or override the draft values -----------------
            var (typed, notAccepted) = await requestContext.Server.TryElicit(
                new GraphNewFolder { Name = name, ContentTypeId = contentTypeId },
                cancellationToken);
            if (notAccepted != null) return notAccepted;

            // -- Build the request body ------------------------------------------------
            var folderItem = new DriveItem
            {
                Name = typed!.Name,
                Folder = new Folder(),
                AdditionalData = new Dictionary<string, object>
                {
                    ["@microsoft.graph.conflictBehavior"] = "rename"
                }
            };

            if (!string.IsNullOrWhiteSpace(typed.ContentTypeId))
            {
                folderItem.AdditionalData["contentType"] = new Dictionary<string, object>
                {
                    ["id"] = typed.ContentTypeId!
                };
            }



            var itemPath = string.IsNullOrWhiteSpace(parentPath)
                ? "/root/children"
                : $"/root:/{parentPath.Trim('/')}:/children";
    var requestInfo = new RequestInformation
    {
        HttpMethod = Method.POST,
        UrlTemplate = "https://graph.microsoft.com/beta/drives/{driveId}/root/children",
        PathParameters = new Dictionary<string, object> { { "driveId", driveId } },
        Content = BinaryData.FromObjectAsJson(new { name = "Test123456", folder = new { } }).ToStream()
    };
            var requestInfo2 = new RequestInformation
            {
                HttpMethod = Method.POST,
                UrlTemplate = $"https://graph.microsoft.com/beta/drives/{{driveId}}{itemPath}",
                PathParameters = new Dictionary<string, object> { { "driveId", driveId } },
                Content = BinaryData.FromObjectAsJson(folderItem).ToStream()
            };

            // Hier voeg je de vereiste header toe!
            if (!string.IsNullOrWhiteSpace(typed.ContentTypeId))
            {
                requestInfo.Headers.Add("Prefer", "IdType=\"ImmutableId\"");
            }

            try
            {
                var created = await client.RequestAdapter.SendAsync<DriveItem>(
                    requestInfo,
                    DriveItem.CreateFromDiscriminatorValue,
                    cancellationToken: cancellationToken);




                // -- Build a friendly path for the response ---------------------------
                var fullPath = string.IsNullOrWhiteSpace(parentPath)
                             ? typed.Name
                             : $"{parentPath.TrimEnd('/')}/{typed.Name}";

                return created
                    .ToJsonContentBlock(
                        $"https://graph.microsoft.com/beta/drives/{driveId}/root:/{fullPath}")
                    .ToCallToolResult();
            }
            catch (Exception ex)
            {
                return JsonSerializer.Serialize(new { error = ex.Message, exception = ex.ToString() })
                                     .ToTextCallToolResponse();
            }
        }*/
    // }




    [Description("Please fill in the new File details.")]
    public class GraphUploadFile
    {
        [JsonPropertyName("name")]
        [Required]
        [Description("The name of the new file.")]
        public string Name { get; set; } = default!;

        [JsonPropertyName("path")]
        [Required]
        [Description("The path of the new file.")]
        public string Path { get; set; } = default!;

        [JsonPropertyName("content")]
        [Required]
        [Description("The content of the new file.")]
        public string Content { get; set; } = default!;

    }

    [Description("Please fill in the new Folder details.")]
    public class GraphNewFolder
    {
        [JsonPropertyName("name")]
        [Required]
        [Description("The name of the new folder.")]
        public string Name { get; set; } = default!;

        [JsonPropertyName("contentTypeId")]
        [Description("The id of the content type.")]
        public string? ContentTypeId { get; set; }
    }
}