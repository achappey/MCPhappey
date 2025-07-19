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
        [Description("Displayname of the new channel")]
        string displayName,
        IServiceProvider serviceProvider,
        RequestContext<CallToolRequestParams> requestContext,
        TeamVisibilityType? teamVisibilityType = TeamVisibilityType.Private,
        [Description("Description of the new channel")]
        string? description = null,
        CancellationToken cancellationToken = default)
    {
        var dto = await requestContext.Server.GetElicitResponse(new GraphNewTeam()
        {
            DisplayName = displayName,
            Description = description,
            Visibility = teamVisibilityType ?? TeamVisibilityType.Private,
        }, cancellationToken);

        var newTeam = new Team
        {
            Visibility = dto?.Visibility,
            DisplayName = dto?.DisplayName,
            Description = dto?.Description,
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

        var resultForm = await requestContext.Server.GetElicitResponse(new GraphNewTeamChannel()
        {
            DisplayName = displayName,
            Description = description,
            MembershipType = membershipType ?? ChannelMembershipType.Standard
        }, cancellationToken);

        var newItem = new Channel
        {
            DisplayName = resultForm.DisplayName,
            Description = resultForm.Description,
            MembershipType = resultForm.MembershipType
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