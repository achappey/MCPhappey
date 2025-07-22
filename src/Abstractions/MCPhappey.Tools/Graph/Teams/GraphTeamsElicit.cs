using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Microsoft.Graph.Beta.Models;

namespace MCPhappey.Tools.Graph.Teams;

[Description("Please fill in the Team details.")]
public class GraphNewTeam
{
    [JsonPropertyName("displayName")]
    [Required]
    [Description("The team display name.")]
    public string DisplayName { get; set; } = default!;

    [JsonPropertyName("description")]
    [Description("The team description.")]
    public string? Description { get; set; }

    [JsonPropertyName("firstChannelName")]
    [Description("The team first channel name.")]
    public string? FirstChannelName { get; set; }

    [JsonPropertyName("visibility")]
    [Description("The team visibility.")]
    public TeamVisibilityType Visibility { get; set; }

    [JsonPropertyName("allowMembersCreateUpdateChannels")]
    [Description("If members are allowed to create and update channels.")]
    [DefaultValue(true)]
    public bool AllowCreateUpdateChannels { get; set; } = true;

}

[Description("Please fill in the Team channel details.")]
public class GraphNewTeamChannel
{
    [JsonPropertyName("displayName")]
    [Required]
    [Description("The team channel display name.")]
    public string DisplayName { get; set; } = default!;

    [JsonPropertyName("description")]
    [Description("The team channel description.")]
    public string? Description { get; set; }

    [JsonPropertyName("membershipType")]
    [Required]
    [Description("The team channel membership type.")]
    public ChannelMembershipType MembershipType { get; set; }
}

[Description("Please fill in the Team channel message details.")]
public class GraphNewChannelMessage
{
    [JsonPropertyName("subject")]
    [Required]
    [Description("Subject of the channel message.")]
    public string? Subject { get; set; }

    [JsonPropertyName("content")]
    [Required]
    [Description("Content of the channel message.")]
    public string? Content { get; set; }

    [JsonPropertyName("importance")]
    [Required]
    [Description("Importance of the channel message.")]
    public ChatMessageImportance? Importance { get; set; }

}