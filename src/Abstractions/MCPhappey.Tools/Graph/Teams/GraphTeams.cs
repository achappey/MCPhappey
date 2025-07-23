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
    public static async Task<CallToolResult?> GraphTeams_CreateTeam(
        [Description("Displayname of the new channel")]
        string displayName,
        IServiceProvider serviceProvider,
        RequestContext<CallToolRequestParams> requestContext,
        TeamVisibilityType? teamVisibilityType = TeamVisibilityType.Private,
        [Description("Description of the new channel")]
        string? description = null,
        CancellationToken cancellationToken = default)
    {
        var (typed, notAccepted) = await requestContext.Server.TryElicit(
         new GraphNewTeam
         {
             DisplayName = displayName,
             Description = description,
             Visibility = teamVisibilityType ?? TeamVisibilityType.Private
         },
         cancellationToken
     );
        if (notAccepted != null) return notAccepted;

        var newTeam = new Team
        {
            Visibility = typed?.Visibility,
            DisplayName = typed?.DisplayName,
            Description = typed?.Description,
            AdditionalData = new Dictionary<string, object>
            {
                {
                    "template@odata.bind" , "https://graph.microsoft.com/beta/teamsTemplates('standard')"
                },
            },
        };

        var client = await serviceProvider.GetOboGraphClient(requestContext.Server);
        var result = await client.Teams.PostAsync(newTeam, cancellationToken: cancellationToken);

        return (result ?? newTeam).ToJsonContentBlock("https://graph.microsoft.com/beta/teams").ToCallToolResult();
    }

    [Description("Create a new channel in a Microsoft Teams.")]
    [McpServerTool(Name = "GraphTeams_CreateChannel", ReadOnly = false, OpenWorld = false)]
    public static async Task<CallToolResult?> GraphTeams_CreateChannel(
        string teamId,
         [Description("Displayname of the new channel")]
        string displayName,
        IServiceProvider serviceProvider,
        RequestContext<CallToolRequestParams> requestContext,
         [Description("Description of the new channel")]
        string? description = null,
        //  ChannelMembershipType? membershipType = ChannelMembershipType.Standard,
        CancellationToken cancellationToken = default)
    {
        var client = await serviceProvider.GetOboGraphClient(requestContext.Server);
        var teams = await client.Teams[teamId]
                           .GetAsync(cancellationToken: cancellationToken);

        var (typed, notAccepted) = await requestContext.Server.TryElicit(
            new GraphNewTeamChannel
            {
                DisplayName = displayName,
                Description = description,
                MembershipType = ChannelMembershipType.Standard
            },
            cancellationToken
        );

        if (notAccepted != null) return notAccepted;
        if (typed == null) return "Invalid result".ToErrorCallToolResponse();

        var newItem = new Channel
        {
            DisplayName = typed.DisplayName,
            Description = typed.Description,
            MembershipType = typed.MembershipType
        };

        var result = await client.Teams[teamId].Channels.PostAsync(newItem, cancellationToken: cancellationToken);

        return (result ?? newItem).ToJsonContentBlock($"https://graph.microsoft.com/beta/teams/{teamId}/channels").ToCallToolResult();
    }

    [Description("Create a new channel message in a Microsoft Teams channel.")]
    [McpServerTool(Name = "GraphTeams_CreateChannelMessage", ReadOnly = false, Destructive = false, OpenWorld = false)]
    public static async Task<CallToolResult?> GraphTeams_CreateChannelMessage(
        [Description("ID of the Team.")] string teamId,
        [Description("ID of the Channel.")] string channelId,
        IServiceProvider serviceProvider,
        RequestContext<CallToolRequestParams> requestContext,
        [Description("Subject of the message.")] string? subject = null,
        [Description("Content (body) of the message.")] string? content = null,
        CancellationToken cancellationToken = default)
    {
        // Vul defaults uit de parameters direct in
        var (typed, notAccepted) = await requestContext.Server.TryElicit(
            new GraphNewChannelMessage
            {
                Subject = subject,
                Content = content,
                Importance = ChatMessageImportance.Normal
            },
            cancellationToken
        );

        if (notAccepted != null) return notAccepted;

        var newItem = new ChatMessage
        {
            Subject = typed?.Subject,
            Importance = typed?.Importance,
            Body = new ItemBody
            {
                Content = typed?.Content,
            },
        };

        var client = await serviceProvider.GetOboGraphClient(requestContext.Server);
        var result = await client.Teams[teamId]
            .Channels[channelId]
            .Messages
            .PostAsync(newItem, cancellationToken: cancellationToken);

        return (result ?? newItem)
            .ToJsonContentBlock($"https://graph.microsoft.com/beta/teams/{teamId}/channels/{channelId}/messages").ToCallToolResult();
    }

    [Description("Create a reply to a Teams channel message, mentioning specified users.")]
    [McpServerTool(Name = "GraphTeams_ReplyWithMentions", ReadOnly = false, Destructive = false, OpenWorld = false)]
    public static async Task<CallToolResult?> GraphTeams_ReplyWithMentions(
        [Description("ID of the Team.")] string teamId,
        [Description("ID of the Channel.")] string channelId,
        [Description("ID of the message to reply to.")] string messageId,
        [Description("IDs of the users to mention.")] List<string> mentionUserIds,
        IServiceProvider serviceProvider,
        RequestContext<CallToolRequestParams> requestContext,
        [Description("Optional extra message after mentions.")] string? content = null,
        CancellationToken cancellationToken = default)
    {
        var client = await serviceProvider.GetOboGraphClient(requestContext.Server);

        var mentionInfo = new List<(string Id, string DisplayName)>();
        foreach (var userId in mentionUserIds)
        {
            var user = await client.Users[userId].GetAsync(cancellationToken: cancellationToken);
            mentionInfo.Add((userId, user?.DisplayName ?? userId));
        }

        var mentionList = string.Join("\n", mentionInfo.Select(x => $"- {x.DisplayName}"));
        var elicit = await requestContext.Server.ElicitAsync(new ElicitRequestParams()
        {
            Message = mentionList
        }, cancellationToken: cancellationToken);

        if (elicit.Action != "accept")
        {
            return elicit.Action.ToErrorCallToolResponse();
        }

        // Resolve display names for user IDs (helper function, see below)
        var mentions = new List<ChatMessageMention>();
        var mentionTags = new List<string>();

        int idx = 0;
        foreach (var (userId, displayName) in mentionInfo)
        {
            mentionTags.Add($"<at id=\"{idx}\">{displayName}</at>");
            mentions.Add(new ChatMessageMention
            {
                Id = idx,
                MentionText = displayName,
                Mentioned = new ChatMessageMentionedIdentitySet
                {
                    User = new Identity
                    {
                        Id = userId,
                        DisplayName = displayName
                    }
                }
            });
            idx++;
        }

        var bodyContent = string.Join(", ", mentionTags);
        if (!string.IsNullOrWhiteSpace(content))
            bodyContent += " " + content;

        var newReply = new ChatMessage
        {
            Body = new ItemBody
            {
                ContentType = BodyType.Html,
                Content = bodyContent
            },
            Mentions = mentions
        };

        var result = await client.Teams[teamId]
            .Channels[channelId]
            .Messages[messageId]
            .Replies
            .PostAsync(newReply, cancellationToken: cancellationToken);

        return (result ?? newReply)
            .ToJsonContentBlock($"https://graph.microsoft.com/beta/teams/{teamId}/channels/{channelId}/messages/{messageId}/replies")
            .ToCallToolResult();
    }


}