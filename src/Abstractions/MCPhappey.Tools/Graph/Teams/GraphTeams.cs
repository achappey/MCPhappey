using System.ComponentModel;
using MCPhappey.Common.Extensions;
using MCPhappey.Core.Extensions;
using Microsoft.Graph.Beta.Models;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace MCPhappey.Tools.Graph.Teams;

public static partial class GraphTeams
{
    [Description("Create a new Microsoft Teams.")]
    [McpServerTool(Title = "Create Microsoft Teams",
        Destructive = true,
        OpenWorld = false)]
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

        using var client = await serviceProvider.GetOboGraphClient(requestContext.Server);
        var result = await client.Teams.PostAsync(newTeam, cancellationToken: cancellationToken);

        return (result ?? newTeam).ToJsonContentBlock("https://graph.microsoft.com/beta/teams").ToCallToolResult();
    }
}