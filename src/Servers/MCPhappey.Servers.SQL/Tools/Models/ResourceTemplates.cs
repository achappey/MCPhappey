
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace MCPhappey.Servers.SQL.Tools.Models;

[Description("Please confirm the name of the resource template you want to delete.")]
public class ConfirmDeleteResourceTemplate
{
    [JsonPropertyName("name")]
    [Required]
    [Description("Enter the exact name of the resource template to confirm deletion.")]
    public string Name { get; set; } = default!;
}

[Description("Update one or more fields. Leave blank to skip updating that field. Use a single space to clear the value.")]
public class UpdateMcpResourceTemplate
{
    [JsonPropertyName("uriTemplate")]
    [Description("New uri template of the resource template (optional).")]
    public string? UriTemplate { get; set; }

    [JsonPropertyName("description")]
    [Description("New description of the resource template (optional).")]
    public string? Description { get; set; }
}

[Description("Please fill in the details to add a new resource template to the specified MCP server.")]
public class AddMcpResourceTemplate
{
    [JsonPropertyName("uri")]
    [Required]
    [Description("The URI of the resource template to add.")]
    public string UriTemplate { get; set; } = default!;

    [JsonPropertyName("name")]
    [Required]
    [Description("The name of the resource template to add.")]
    public string Name { get; set; } = default!;

    [JsonPropertyName("description")]
    [Description("Optional description of the resource template.")]
    public string? Description { get; set; }
}