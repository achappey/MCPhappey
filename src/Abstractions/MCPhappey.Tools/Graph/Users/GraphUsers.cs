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
    [McpServerTool(Name = "GraphUsers_CreateUser", ReadOnly = false)]
    public static async Task<CallToolResult?> GraphUsers_CreateUser(
      IServiceProvider serviceProvider,
      RequestContext<CallToolRequestParams> requestContext,
      [Description("The users's given name.")] string? givenName = null,
      [Description("The users's display name.")] string? displayName = null,
      [Description("The users's principal name.")] string? userPrincipalName = null,
      [Description("The users's mail nickname.")] string? mailNickname = null,
      [Description("Account enabled.")] bool? accountEnabled = null,
      [Description("Force password change.")] bool? forceChangePasswordNextSignIn = null,
      [Description("The users's password.")] string? password = null,
      CancellationToken cancellationToken = default)
    {
        var mcpServer = requestContext.Server;

        var (typed, notAccepted) = await mcpServer.TryElicit<GraphNewUser>(
            new GraphNewUser
            {
                GivenName = givenName,
                DisplayName = displayName ?? string.Empty,
                UserPrincipalName = userPrincipalName ?? string.Empty,
                MailNickname = mailNickname ?? string.Empty,
                AccountEnabled = accountEnabled ?? true,
                ForceChangePasswordNextSignIn = forceChangePasswordNextSignIn ?? true,
                Password = password ?? string.Empty
            },
            cancellationToken
        );
        if (notAccepted != null) return notAccepted;

        var client = await serviceProvider.GetOboGraphClient(mcpServer);
        var user = new User()
        {
            DisplayName = typed?.DisplayName,
            GivenName = typed?.GivenName,
            MailNickname = typed?.MailNickname,
            AccountEnabled = typed?.AccountEnabled,
            PasswordProfile = new PasswordProfile()
            {
                ForceChangePasswordNextSignIn = typed?.ForceChangePasswordNextSignIn,
                Password = typed?.Password
            },
            UserPrincipalName = typed?.UserPrincipalName
        };

        var newUser = await client.Users.PostAsync(user, cancellationToken: cancellationToken);

        return newUser.ToJsonContentBlock($"https://graph.microsoft.com/beta/users/{newUser.Id}").ToCallToolResult();
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
