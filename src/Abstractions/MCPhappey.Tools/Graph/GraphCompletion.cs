using MCPhappey.Common;
using MCPhappey.Common.Models;
using ModelContextProtocol.Server;
using ModelContextProtocol.Protocol;
using MCPhappey.Core.Extensions;

namespace MCPhappey.Tools.Graph;

public class GraphCompletion : IAutoCompletion
{
    public bool SupportsHost(ServerConfig serverConfig)
        => serverConfig.Server.ServerInfo.Name.StartsWith("Microsoft-");

    public async Task<CompleteResult?> GetCompletion(
     IMcpServer mcpServer,
     IServiceProvider serviceProvider,
     CompleteRequestParams? completeRequestParams,
     CancellationToken cancellationToken = default)
    {
        if (completeRequestParams?.Argument?.Name is not string argName || completeRequestParams.Argument.Value is not string argValue)
            return new CompleteResult();

        var client = await serviceProvider.GetOboGraphClient(mcpServer);

        IEnumerable<string> result = [];

        switch (completeRequestParams.Argument.Name)
        {
            case "teamName":
                var teams = await client.Teams.GetAsync((requestConfiguration) =>
                {
                    if (!string.IsNullOrWhiteSpace(argValue))
                    {
                        requestConfiguration.QueryParameters.Filter = $"startswith(displayName,'{argValue.Replace("'", "''")}')";
                    }

                    requestConfiguration.QueryParameters.Top = 100;
                }, cancellationToken);

                result = teams?.Value?.Select(a => a.DisplayName)
                                            .OfType<string>()
                                            .ToList() ?? [];
                break;
            case "userPrincipalName":
                // UPN/email; returns userPrincipalName for autocompletion
                var userNameUsers = await client.Users.GetAsync(requestConfiguration =>
                {
                    if (!string.IsNullOrWhiteSpace(argValue))
                        requestConfiguration.QueryParameters.Filter = $"startswith(userPrincipalName,'{argValue.Replace("'", "''")}')";
                    requestConfiguration.QueryParameters.Top = 100;
                    requestConfiguration.QueryParameters.Select = ["userPrincipalName"];
                }, cancellationToken);

                result = userNameUsers?.Value?
                            .Select(u => u.UserPrincipalName)
                            .OfType<string>()
                            .Order()
                            .Take(100)
                            .ToList() ?? [];
                break;


            case "userDisplayName":
                // DisplayName; returns DisplayName for autocompletion
                var displayNameUsers = await client.Users.GetAsync(requestConfiguration =>
                {
                    if (!string.IsNullOrWhiteSpace(argValue))
                        requestConfiguration.QueryParameters.Filter = $"startswith(displayName,'{argValue.Replace("'", "''")}')";
                    requestConfiguration.QueryParameters.Top = 100;
                    requestConfiguration.QueryParameters.Select = ["displayName"];
                }, cancellationToken);

                result = displayNameUsers?.Value?
                            .Select(u => u.DisplayName)
                            .OfType<string>()
                            .Order()
                            .ToList() ?? [];
                break;

            case "departmentName":
                var users = await client.Users.GetAsync(requestConfiguration =>
                {
                    requestConfiguration.QueryParameters.Top = 999;
                    requestConfiguration.QueryParameters.Select = ["department"];
                }, cancellationToken);

                result = users?.Value?
                    .Where(u => !string.IsNullOrWhiteSpace(u.Department))
                    .GroupBy(u => u.Department)
                    .OrderByDescending(g => g.Count())
                    .Select(g => g.Key)
                    .OfType<string>()
                    .Order()
                    .Take(100)
                    .ToList() ?? [];

                // Optionally filter by argValue for autocomplete
                if (!string.IsNullOrWhiteSpace(argValue))
                    result = [.. result.Where(d => d.Contains(argValue, StringComparison.OrdinalIgnoreCase))];

                break;

            case "plannerName":
                var plans = await client.Me.Planner.Plans.GetAsync(cancellationToken: cancellationToken);

                var items = plans?.Value ?? [];
                if (!string.IsNullOrWhiteSpace(argValue))
                    items = [.. items.Where(d => d.Title?.Contains(argValue, StringComparison.OrdinalIgnoreCase) == true)];

                result = items.Select(p => p.Title)
                                        .OfType<string>()
                                        .Take(100)
                                        .ToList() ?? [];
                break;

            case "groupName":
                var groups = await client.Groups.GetAsync((requestConfiguration) =>
                {
                    if (!string.IsNullOrWhiteSpace(argValue))
                    {
                        requestConfiguration.QueryParameters.Filter = $"startswith(displayName,'{argValue.Replace("'", "''")}')";
                    }
                    requestConfiguration.QueryParameters.Top = 100;
                }, cancellationToken);

                result = groups?.Value?.Select(g => g.DisplayName)
                                        .OfType<string>()
                                        .ToList() ?? [];
                break;

            default:
                break;
        }

        return new CompleteResult()
        {
            Completion = new Completion()
            {
                Values = [.. result]
            }
        };

    }

}
