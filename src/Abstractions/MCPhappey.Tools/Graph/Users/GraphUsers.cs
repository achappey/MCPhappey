using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
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
        var client = await serviceProvider.GetOboGraphClient(mcpServer);

        var elicitParams = "Please fill in the user details".CreateElicitRequestParamsForType<GraphNewUser>();

        var elicitResult = await requestContext.Server.ElicitAsync(elicitParams, cancellationToken: cancellationToken);
        elicitResult.EnsureAccept();

        var dto = JsonSerializer.Deserialize<GraphNewUser>(
                 JsonSerializer.Serialize(elicitResult.Content)
            );

        var result = await client.Users.PostAsync(new User()
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
        }, cancellationToken: cancellationToken);

        return result.ToJsonContentBlock("https://graph.microsoft.com/beta/users");
    }

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
