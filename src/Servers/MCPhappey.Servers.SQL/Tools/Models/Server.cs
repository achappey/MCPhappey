
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace MCPhappey.Servers.SQL.Tools.Models;

[Description("Please fill in the MCP Server details.")]
public class NewMcpServer
{
    [JsonPropertyName("name")]
    [Required]
    [Description("The MCP server name.")]
    public string Name { get; set; } = default!;

    [JsonPropertyName("instructions")]
    [Description("The MCP server instructions.")]
    public string? Instructions { get; set; }

    [JsonPropertyName("secured")]
    [DefaultValue(true)]
    [Description("If the MCP server is secured and needs authentication")]
    public bool? Secured { get; set; }
}

[Description("Please fill in the MCP Server owner details.")]
public class McpServerOwner
{
    [JsonPropertyName("userId")]
    [Required]
    [Description("The user id of the MCP server owner.")]
    public string UserId { get; set; } = default!;
}

[Description("Please fill in the MCP Server name to confirm deletion.")]
public class DeleteMcpServer
{
    [JsonPropertyName("name")]
    [Required]
    [Description("The MCP server name.")]
    public string Name { get; set; } = default!;
}

[Description("Update one or more fields. Leave blank to skip updating that field. Use a single space to clear the value.")]
public class UpdateMcpServer
{
    [JsonPropertyName("name")]
    [Description("New name of the resource (optional).")]
    public string? Name { get; set; }

    [JsonPropertyName("instructions")]
    [Description("The MCP server instructions.")]
    public string? Instructions { get; set; }
}

[Description("Please fill in the security group details.")]
public class McpSecurityGroup
{
    [JsonPropertyName("groupId")]
    [Required]
    [Description("The object ID of the security group.")]
    public string GroupId { get; set; } = default!;
}

[Description("Update one or more fields. Leave blank to skip updating that field. Use a single space to clear the value.")]
public class UpdateMcpServerSecurity
{
    [JsonPropertyName("secured")]
    [Description("Enable if you would like to secure the MCP server.")]
    public bool? Secured { get; set; }
}