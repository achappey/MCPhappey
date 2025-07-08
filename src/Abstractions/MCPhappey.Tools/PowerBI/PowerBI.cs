using System.ComponentModel;
using Microsoft.PowerBI.Api;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace MCPhappey.Tools.PowerBI;

public static class PowerBI
{
    [Description("Executes a DAX query on a Power BI dataset via the ExecuteQueries endpoint.")]
    [McpServerTool(Name = "PowerBI_ExecuteDaxQuery", ReadOnly = true)]
    public static async Task<ContentBlock?> PowerBI_ExecuteDaxQuery(
        [Description("PowerBI datasetId")] string datasetId,
        [Description("PowerBI DAX query")]
            string daxQuery,
        IServiceProvider serviceProvider,
        RequestContext<CallToolRequestParams> requestContext,
        CancellationToken cancellationToken = default)
    {
        var mcpServer = requestContext.Server;
        PowerBIClient client = await serviceProvider.GetOboPowerBIClient(mcpServer);

        // Build the query request using PowerBI .NET SDK types
        var queryRequest = new Microsoft.PowerBI.Api.Models.DatasetExecuteQueriesRequest
        {
            Queries =
        [
            new Microsoft.PowerBI.Api.Models.DatasetExecuteQueriesQuery { Query = daxQuery }
        ]
        };

        // Call the API (for "My Workspace" datasets)
        var result = await client.Datasets.ExecuteQueriesAsync(datasetId, queryRequest, cancellationToken: cancellationToken);

        return new EmbeddedResourceBlock()
        {
            Resource = new TextResourceContents()
            {
                MimeType = "application/json",
                Text = Newtonsoft.Json.JsonConvert.SerializeObject(result),
                Uri = $"https://api.powerbi.com/v1.0/myorg/datasets/{datasetId}/executeQueries"
            }
        };
    }

}