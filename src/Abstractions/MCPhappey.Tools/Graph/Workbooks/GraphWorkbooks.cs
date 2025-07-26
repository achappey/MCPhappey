using System.ComponentModel;
using MCPhappey.Common.Extensions;
using MCPhappey.Core.Extensions;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace MCPhappey.Tools.Graph.Workbooks;

public static partial class GraphWorkbooks
{
    [Description("Add a new row to an Excel table on OneDrive/SharePoint. Use defaultValues dictionary to add default values to the Excel new row form.")]
    [McpServerTool(
        Name = "GraphWorkbooks_AddRowToTable",
        Title = "Add row to Excel table",
        OpenWorld = false)]
    public static async Task<CallToolResult?> GraphWorkbooks_AddRowToTable(
        string driveId,
        string itemId,
        string tableName,
        IServiceProvider serviceProvider,
        RequestContext<CallToolRequestParams> requestContext,
        [Description("Default values for the row form. Format: key is row name, value is default value.")]
            Dictionary<string, string>? defaultValues = null,
        CancellationToken cancellationToken = default)
    {
        // 1. Haal de kolomnamen van de Excel-tabel op
        var graphClient = await serviceProvider.GetOboGraphClient(requestContext.Server);

        try
        {
            var columnsResponse = await graphClient.Drives[driveId].Items[itemId]
                .Workbook
                .Tables[tableName].Columns
                .GetAsync(cancellationToken: cancellationToken);

            var columns = columnsResponse?.Value?.Select(col => col.Name).OfType<string>().ToList();

            if (defaultValues == null)
            {
                return $"defaultValues missing. Please provide some default values. Column names: {string.Join(",", columns ?? [])}"
                    .ToErrorCallToolResponse();
            }

            // 2. Vraag de gebruiker om input per kolom (elicit)
            var elicited = await requestContext.Server.ElicitAsync(new ElicitRequestParams()
            {
                Message = "Please fill in the values of the Excel table".ToElicitDefaultData(defaultValues),
                RequestedSchema = new ElicitRequestParams.RequestSchema()
                {
                    Properties = columns?.ToDictionary(
                            a => a,
                            a => (ElicitRequestParams.PrimitiveSchemaDefinition)new ElicitRequestParams.StringSchema
                            {
                                Title = a,
                            }
                        ) ?? [],
                }

            }, cancellationToken);

            if (elicited.Action != "accept")
            {
                return elicited.Action.ToErrorCallToolResponse();
            }
            var valuesDict = ExtractValues(elicited.Content);
            var valuesNode = BuildValuesNode(columns!, valuesDict);

            var newRow = await graphClient.Drives[driveId].Items[itemId]
                .Workbook.Tables[tableName].Rows
                .PostAsync(new Microsoft.Graph.Beta.Models.WorkbookTableRow
                {
                    Values = valuesNode
                }, cancellationToken: cancellationToken);

            var workbookGraphUrl = $"https://graph.microsoft.com/beta/drives/{driveId}/items/{itemId}/workbook";

            return elicited.Content.ToJsonContentBlock(workbookGraphUrl).ToCallToolResult();
        }
        catch (Exception e)
        {
            return e.Message.ToErrorCallToolResponse();
        }
    }
}
