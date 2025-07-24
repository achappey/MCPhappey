
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace MCPhappey.Servers.SQL.Tools.Models;

[Description("Please confirm the name of the resource you want to delete.")]
public class ConfirmDeleteResource
{
    [JsonPropertyName("name")]
    [Required]
    [Description("Enter the exact name of the resource to confirm deletion.")]
    public string Name { get; set; } = default!;
}

[Description("Update the resource.")]
public class UpdateMcpResource
{
    [JsonPropertyName("uri")]
    [Description("The URI of the resource (optional).")]
    public string? Uri { get; set; }

    [JsonPropertyName("title")]
    [Description("The resource title.")]
    public string? Title { get; set; }

    [JsonPropertyName("description")]
    [Description("New description of the resource (optional).")]
    public string? Description { get; set; }
}


[Description("Please fill in the details to add a new resource to the specified MCP server.")]
public class AddMcpResource
{
    [JsonPropertyName("uri")]
    [Required]
    [Description("The URI of the resource to add.")]
    public string Uri { get; set; } = default!;

    [JsonPropertyName("name")]
    [Required]
    [Description("The name of the resource to add.")]
    public string Name { get; set; } = default!;

    [JsonPropertyName("title")]
    [Description("The resource title.")]
    public string? Title { get; set; }

    [JsonPropertyName("description")]
    [Description("Optional description of the resource.")]
    public string? Description { get; set; }
}