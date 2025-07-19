
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace MCPhappey.Servers.SQL.Tools.Models;

[Description("Please confirm the name of the prompt you want to delete.")]
public class ConfirmDeletePrompt
{
    [JsonPropertyName("name")]
    [Required]
    [Description("Enter the exact name of the prompt to confirm deletion.")]
    public string Name { get; set; } = default!;
}

[Description("Update one or more fields. Leave blank to skip updating that field. Use a single space to clear the value.")]
public class UpdateMcpPrompt
{
    [JsonPropertyName("prompt")]
    [Description("New prompt (optional).")]
    public string? Prompt { get; set; }

    [JsonPropertyName("description")]
    [Description("New description of the prompt (optional).")]
    public string? Description { get; set; }
}

[Description("Update one or more fields. Leave blank to skip updating that field. Use a single space to clear the value.")]
public class UpdateMcpPromptArgument
{
    [JsonPropertyName("description")]
    [Description("New description of the prompt argument (optional).")]
    public string? Description { get; set; }

    [JsonPropertyName("required")]
    [Description("If the argument is required (optional).")]
    public bool? Required { get; set; }
}

[Description("Please fill in the details to add a new prompt to the specified MCP server.")]
public class AddMcpPrompt
{
    [JsonPropertyName("name")]
    [Required]
    [Description("The name of the resource to add.")]
    public string Name { get; set; } = default!;

    [JsonPropertyName("prompt")]
    [Required]
    [Description("The prompt to add. You can use {argument} style placeholders for prompt arguments.")]
    public string Prompt { get; set; } = default!;

    [JsonPropertyName("description")]
    [Description("Optional description of the resource.")]
    public string? Description { get; set; }
}