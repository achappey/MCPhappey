using MCPhappey.Common;
using MCPhappey.Common.Models;
using MCPhappey.Simplicate.Options;
using ModelContextProtocol.Server;
using ModelContextProtocol.Protocol;
using MCPhappey.Core.Services;
using MCPhappey.Simplicate.Extensions;
using System.Text.Json.Serialization;

namespace MCPhappey.Simplicate;

public class SimplicateCompletion(
    SimplicateOptions simplicateOptions,
    DownloadService downloadService) : IAutoCompletion
{
    public bool SupportsHost(ServerConfig serverConfig)
        => serverConfig.Server.ServerInfo.Name.StartsWith("Simplicate-");

    public async Task<CompleteResult?> GetCompletion(IMcpServer mcpServer, IServiceProvider serviceProvider, CompleteRequestParams? completeRequestParams, CancellationToken cancellationToken = default)
    {
        switch (completeRequestParams?.Argument.Name)
        {
            case "teamNaam":
                string teamUrl = $"https://{simplicateOptions.Organization}.simplicate.app/api/v2/hrm/team?q[name]={completeRequestParams.Argument.Value}*&sort=name&select=name";
                var items = await downloadService.GetSimplicatePageAsync<SimplicateNameItem>(serviceProvider,
                    mcpServer, teamUrl, cancellationToken);

                return new CompleteResult()
                {
                    Completion = new()
                    {
                        Values = items?.Data.Select(z => z.Name)
                            .ToList() ?? []
                    }
                };

            case "projectNaam":
                string projectUrl = $"https://{simplicateOptions.Organization}.simplicate.app/api/v2/projects/project?q[name]={completeRequestParams.Argument.Value}*&sort=name&select=name";
                var projectItems = await downloadService.GetSimplicatePageAsync<SimplicateNameItem>(serviceProvider,
                    mcpServer, projectUrl, cancellationToken);

                return new CompleteResult()
                {
                    Completion = new()
                    {
                        Values = projectItems?.Data.Select(z => z.Name)
                            .ToList() ?? []
                    }
                };
            case "medewerkerNaam":
                string employeeUrl = $"https://{simplicateOptions.Organization}.simplicate.app/api/v2/hrm/employee?q[name]={completeRequestParams.Argument.Value}*&sort=name&select=name&q[is_user]=true";
                var employeeItems = await downloadService.GetSimplicatePageAsync<SimplicateNameItem>(serviceProvider,
                    mcpServer, employeeUrl, cancellationToken);

                return new CompleteResult()
                {
                    Completion = new()
                    {
                        Values = employeeItems?.Data.Select(z => z.Name)
                            .ToList() ?? []
                    }
                };
            default:
                return new CompleteResult();
        }
    }

    public class SimplicateNameItem
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
    }

}
