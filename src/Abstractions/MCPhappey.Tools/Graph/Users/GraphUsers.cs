using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using MCPhappey.Common.Extensions;
using MCPhappey.Core.Extensions;
using Microsoft.Graph.Beta.Models;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace MCPhappey.Tools.Graph.Users;

public static class GraphUsers
{
    [Description("Create a new user")]
    [McpServerTool(ReadOnly = false)]
    public static async Task<ContentBlock?> GraphUsers_CreateUser(
        IServiceProvider serviceProvider,
        RequestContext<CallToolRequestParams> requestContext,
        CancellationToken cancellationToken = default)
    {
        var mcpServer = requestContext.Server;

        var dto = await requestContext.Server.GetElicitResponse<GraphNewUser>(cancellationToken);
        var client = await serviceProvider.GetOboGraphClient(mcpServer);
        var user = new User()
        {
            DisplayName = dto?.DisplayName,
            GivenName = dto?.GivenName,
            MailNickname = dto?.MailNickname,
            AccountEnabled = dto?.AccountEnabled,
            PasswordProfile = new PasswordProfile()
            {
                ForceChangePasswordNextSignIn = dto?.ForceChangePasswordNextSignIn,
                Password = dto?.Password
            },
            UserPrincipalName = dto?.UserPrincipalName
        };

        await client.Users.PostAsync(user, cancellationToken: cancellationToken);

        return user.ToJsonContentBlock("https://graph.microsoft.com/beta/users");
    }


    [Description("Please fill in the user details.")]
    public class GraphNewUser
    {
        [JsonPropertyName("givenName")]
        [Description("The users's given name.")]
        public string? GivenName { get; set; }

        [JsonPropertyName("displayName")]
        [Required]
        [Description("The users's display name.")]
        public string DisplayName { get; set; } = default!;

        [JsonPropertyName("userPrincipalName")]
        [Required]
        [EmailAddress]
        [Description("The users's principal name.")]
        public string UserPrincipalName { get; set; } = default!;

        [JsonPropertyName("mailNickname")]
        [Required]
        [Description("The users's mail nickname.")]
        public string MailNickname { get; set; } = default!;

        [JsonPropertyName("accountEnabled")]
        [Required]
        [DefaultValue(true)]
        [Description("Account enabled.")]
        public bool AccountEnabled { get; set; }

        [JsonPropertyName("forceChangePasswordNextSignIn")]
        [Required]
        [DefaultValue(true)]
        [Description("Force password change.")]
        public bool ForceChangePasswordNextSignIn { get; set; }

        [JsonPropertyName("password")]
        [Required]
        [Description("The users's password.")]
        public string Password { get; set; } = default!;

    }
}
