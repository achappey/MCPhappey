using System.ComponentModel;
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
                    "template@odata.bind" , "https://graph.microsoft.com/beta/teamsTemplates('standard')"
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
         [Description("Displayname of the new channel")]
        string displayName,
        IServiceProvider serviceProvider,
        RequestContext<CallToolRequestParams> requestContext,
         [Description("Description of the new channel")]
        string? description = null,
        ChannelMembershipType? membershipType = ChannelMembershipType.Standard,
        CancellationToken cancellationToken = default)
    {
        var client = await serviceProvider.GetOboGraphClient(requestContext.Server);
        var teams = await client.Teams[teamId]
                           .GetAsync(cancellationToken: cancellationToken);

        // Elicit details for the new list
        var values = new Dictionary<string, string>
        {
            { "Teams", teams?.DisplayName ?? teams?.WebUrl ?? string.Empty },
            { "Displayname", displayName },
            { "Description", description ?? string.Empty },
            { "Membership type", membershipType.ToString()?? string.Empty}
        };

        // AI-native: Elicit message alleen voor confirm/review
        await requestContext.Server.GetElicitResponse(values, cancellationToken);
        var newItem = new Channel
        {
            DisplayName = displayName,
            Description = description,
            MembershipType = membershipType
        };

        var result = await client.Teams[teamId].Channels.PostAsync(newItem, cancellationToken: cancellationToken);

        return (result ?? newItem).ToJsonContentBlock($"https://graph.microsoft.com/beta/teams/{teamId}/channels");
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

}