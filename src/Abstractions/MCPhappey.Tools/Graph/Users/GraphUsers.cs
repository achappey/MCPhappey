using System.ComponentModel;
using MCPhappey.Common.Extensions;
using MCPhappey.Core.Extensions;
using Microsoft.Graph.Beta.Models;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace MCPhappey.Tools.Graph.Users;

public static class GraphUsers
{
    [Description("Create a new user")]
    [McpServerTool(Title = "Create new user", OpenWorld = false)]
    public static async Task<CallToolResult?> GraphUsers_CreateUser(
      IServiceProvider serviceProvider,
      RequestContext<CallToolRequestParams> requestContext,
      [Description("The users's given name.")] string? givenName = null,
      [Description("The users's display name.")] string? displayName = null,
      [Description("The users's principal name.")] string? userPrincipalName = null,
      [Description("The users's mail nickname.")] string? mailNickname = null,
      [Description("The users's job title.")] string? jobTitle = null,
      [Description("The users's mobile phone.")] string? mobilePhone = null,
      [Description("The users's business phone.")] string? businessPhone = null,
      [Description("Account enabled.")] bool? accountEnabled = null,
      [Description("The users's department.")] string? department = null,
      [Description("The users's compay name.")] string? companyName = null,
      [Description("Force password change.")] bool? forceChangePasswordNextSignIn = null,
      [Description("The users's password.")] string? password = null,
      CancellationToken cancellationToken = default)
    {
        var mcpServer = requestContext.Server;

        var (typed, notAccepted) = await mcpServer.TryElicit(
            new GraphNewUser
            {
                GivenName = givenName ?? string.Empty,
                DisplayName = displayName ?? string.Empty,
                UserPrincipalName = userPrincipalName ?? string.Empty,
                MailNickname = mailNickname ?? string.Empty,
                Department = department ?? string.Empty,
                MobilePhone = mobilePhone,
                BusinessPhone = businessPhone,
                CompanyName = companyName ?? string.Empty,
                AccountEnabled = accountEnabled ?? true,
                JobTitle = jobTitle ?? string.Empty,
                ForceChangePasswordNextSignIn = forceChangePasswordNextSignIn ?? true,
                Password = password ?? string.Empty
            },
            cancellationToken
        );

        if (notAccepted != null) return notAccepted;

        using var client = await serviceProvider.GetOboGraphClient(mcpServer);
        var user = new User()
        {
            DisplayName = typed?.DisplayName,
            GivenName = typed?.GivenName,
            MailNickname = typed?.MailNickname,
            JobTitle = typed?.JobTitle,
            CompanyName = typed?.CompanyName,
            Department = typed?.Department,
            MobilePhone = typed?.MobilePhone,
            BusinessPhones = string.IsNullOrWhiteSpace(typed?.BusinessPhone) ? null : [typed.BusinessPhone],
            AccountEnabled = typed?.AccountEnabled,
            PasswordProfile = new PasswordProfile()
            {
                ForceChangePasswordNextSignIn = typed?.ForceChangePasswordNextSignIn,
                Password = typed?.Password
            },
            UserPrincipalName = typed?.UserPrincipalName
        };

        var newUser = await client.Users.PostAsync(user, cancellationToken: cancellationToken);

        return newUser.ToJsonContentBlock($"https://graph.microsoft.com/beta/users/{newUser?.Id}").ToCallToolResult();
    }

    [Description("Update a Microsoft 365 user")]
    [McpServerTool(Title = "Update a user",
        OpenWorld = false)]
    public static async Task<CallToolResult?> GraphUsers_UpdateUser(
        [Description("User id to update.")] string userId,
        IServiceProvider serviceProvider,
        RequestContext<CallToolRequestParams> requestContext,
        [Description("The users's given name.")] string? givenName = null,
        [Description("The users's display name.")] string? displayName = null,
        [Description("The users's job title.")] string? jobTitle = null,
        [Description("The users's compay name.")] string? companyName = null,
        [Description("The users's department.")] string? department = null,
        [Description("The users's mobile phone.")] string? mobilePhone = null,
        [Description("The users's business phone.")] string? businessPhone = null,
        [Description("Account enabled.")] bool? accountEnabled = null,
        CancellationToken cancellationToken = default)
    {
        var mcpServer = requestContext.Server;
        using var client = await serviceProvider.GetOboGraphClient(mcpServer);
        var newUser = await client.Users[userId].GetAsync(cancellationToken: cancellationToken);

        var (typed, notAccepted) = await mcpServer.TryElicit(
            new GraphUpdateUser
            {
                GivenName = givenName ?? newUser?.GivenName ?? string.Empty,
                Department = department ?? newUser?.Department,
                CompanyName = companyName ?? newUser?.CompanyName,
                MobilePhone = mobilePhone ?? newUser?.MobilePhone,
                BusinessPhone = businessPhone ?? newUser?.BusinessPhones?.FirstOrDefault(),
                DisplayName = displayName ?? newUser?.DisplayName ?? string.Empty,
                AccountEnabled = accountEnabled ?? (newUser != null
                    && newUser.AccountEnabled.HasValue && newUser.AccountEnabled.Value),
                JobTitle = jobTitle ?? newUser?.JobTitle ?? string.Empty,
            },
            cancellationToken
        );
        if (notAccepted != null) return notAccepted;

        var user = new User()
        {
            DisplayName = typed?.DisplayName,
            GivenName = typed?.GivenName,
            JobTitle = typed?.JobTitle,
            MobilePhone = typed?.MobilePhone,
            BusinessPhones = string.IsNullOrWhiteSpace(typed?.BusinessPhone) ? null : [typed.BusinessPhone],
            Department = typed?.Department,
            CompanyName = typed?.CompanyName,
            AccountEnabled = typed?.AccountEnabled,
        };

        var patchedUser = await client.Users[userId].PatchAsync(user, cancellationToken: cancellationToken);

        return newUser.ToJsonContentBlock($"https://graph.microsoft.com/beta/users/{patchedUser?.Id}")
             .ToCallToolResult();
    }
}
