using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using MCPhappey.Common.Extensions;
using MCPhappey.Core.Extensions;
using Microsoft.Graph.Beta.Models;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace MCPhappey.Tools.Graph.Teams;

public static class GraphTeams
{
    [Description("Create a new Microsoft Teams.")]
    [McpServerTool(Name = "GraphTeams_CreateTeam", ReadOnly = false, Destructive = false, OpenWorld = false)]
    public static async Task<ContentBlock?> GraphTeams_CreateTeam(
        IServiceProvider serviceProvider,
        RequestContext<CallToolRequestParams> requestContext,
        CancellationToken cancellationToken = default)
    {
        var dto = await requestContext.Server.GetElicitResponse<GraphNewTeam>(cancellationToken);
        var newTeam = new Team
        {
            Visibility = dto?.Visibility,
            DisplayName = dto?.DisplayName,
            Description = dto?.Description,
            MemberSettings = new TeamMemberSettings()
            {
                AllowCreateUpdateChannels = dto?.AllowCreateUpdateChannels
            },
            FirstChannelName = dto?.FirstChannelName,
            AdditionalData = new Dictionary<string, object>
            {
                {
                    "template@odata.bind" , "https://graph.microsoft.com/v1.0/teamsTemplates('standard')"
                },
            },
        };

        var client = await serviceProvider.GetOboGraphClient(requestContext.Server);
        var result = await client.Teams.PostAsync(newTeam, cancellationToken: cancellationToken);

        return (result ?? newTeam).ToJsonContentBlock("https://graph.microsoft.com/beta/teams");
    }

    [Description("Create a new channel in a Microsoft Teams.")]
    [McpServerTool(Name = "GraphTeams_CreateChannel", ReadOnly = false, Destructive = false, OpenWorld = false)]
    public static async Task<ContentBlock?> GraphTeams_CreateChannel(
        string teamId,
        IServiceProvider serviceProvider,
        RequestContext<CallToolRequestParams> requestContext,
        CancellationToken cancellationToken = default)
    {
        var dto = await requestContext.Server.GetElicitResponse<GraphNewTeamChannel>(cancellationToken);
        var newItem = new Channel
        {
            DisplayName = dto?.DisplayName,
            Description = dto?.Description,
            MembershipType = dto?.MembershipType
        };

        var client = await serviceProvider.GetOboGraphClient(requestContext.Server);
        var result = await client.Teams[teamId].Channels.PostAsync(newItem, cancellationToken: cancellationToken);

        return (result ?? newItem).ToJsonContentBlock($"https://graph.microsoft.com/beta/teams/{teamId}/channels");
    }

    [Description("Create a new channel message in a Microsoft Teams channel.")]
    [McpServerTool(Name = "GraphTeams_CreateChannelMessage", ReadOnly = false, Destructive = false, OpenWorld = false)]
    public static async Task<ContentBlock?> GraphTeams_CreateChannelMessage(
        string teamId,
        string channelId,
        IServiceProvider serviceProvider,
        RequestContext<CallToolRequestParams> requestContext,
        CancellationToken cancellationToken = default)
    {
        var dto = await requestContext.Server.GetElicitResponse<GraphNewChannelMessage>(cancellationToken);
        var newItem = new ChatMessage
        {
            Subject = dto?.Subject,
            Importance = dto?.Importance,
            Body = new ItemBody
            {
                Content = dto?.Content,
            },
        };

        var client = await serviceProvider.GetOboGraphClient(requestContext.Server);
        var result = await client.Teams[teamId]
            .Channels[channelId]
            .Messages
            .PostAsync(newItem, cancellationToken: cancellationToken);

        return (result ?? newItem)
            .ToJsonContentBlock($"https://graph.microsoft.com/beta/teams/{teamId}/channels/{channelId}/messages");
    }

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
        [Description("Importance of the channel message.")]
        public ChatMessageImportance? Importance { get; set; }

    }
}