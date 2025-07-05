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
                string baseUrl = $"https://{simplicateOptions.Organization}.simplicate.app/api/v2/hrm/team?q[name]={completeRequestParams.Argument.Value}*";
                var items = await downloadService.GetSimplicatePageAsync<SimplicateTeam>(serviceProvider,
                    mcpServer, baseUrl, cancellationToken);

                return new CompleteResult()
                {
                    Completion = new()
                    {
                        Values = items?.Data.Select(z => z.Name).ToList() ?? []
                    }
                };
            default:
                return new CompleteResult();
        }
    }

    public class SimplicateTeam
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
    }
}
