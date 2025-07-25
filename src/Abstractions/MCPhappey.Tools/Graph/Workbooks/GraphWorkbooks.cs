using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using MCPhappey.Common.Extensions;
using MCPhappey.Core.Extensions;
using Microsoft.Kiota.Abstractions.Serialization;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace MCPhappey.Tools.Graph.Workbooks;

public static class GraphWorkbooks
{
    [Description("Get filtered rows from an Excel table on OneDrive/SharePoint using a 'values' filter (multiple allowed values) via Microsoft Graph.")]
    [McpServerTool(
        Name = "GraphWorkbooks_GetRowsByValuesFilter",
        Title = "Get filtered rows from Excel table by values",
        ReadOnly = true, OpenWorld = false)]
    public static async Task<CallToolResult?> GraphWorkbooks_GetRowsByValuesFilter(
            string driveId,
            string itemId,
            string worksheetName,
            string tableName,
            string filterColumn,
            [Description("List of allowed values for the filter column.")]
            List<string> values,
            IServiceProvider serviceProvider,
            RequestContext<CallToolRequestParams> requestContext,
            CancellationToken cancellationToken = default)
    {
        var mcpServer = requestContext.Server;
        var client = await serviceProvider.GetOboGraphClient(mcpServer);

        try
        {
            // 1. Start session
            var session = await client.Drives[driveId]
                .Items[itemId]
                .Workbook
                .CreateSession
                .PostAsync(new() { PersistChanges = false }, cancellationToken: cancellationToken);

            var sessionId = session?.Id ?? throw new Exception("Failed to create workbook session.");

            // 2. Stel criteria in voor "values"-filter

            // List<string> values komt als parameter binnen
            var untypedValues = new UntypedArray(
                [.. values.Select(v => new UntypedString(v)).Cast<UntypedNode>()]);

            var criteria = new Microsoft.Graph.Beta.Models.WorkbookFilterCriteria
            {
                FilterOn = "values",
                Values = untypedValues        // ← compilet: UntypedArray erft van UntypedNode
            };

            // 3. Pas de filter toe
            await client.Drives[driveId]
                .Items[itemId]
                .Workbook
                .Worksheets[worksheetName]
                .Tables[tableName]
                .Columns[filterColumn]
                .Filter
                .Apply
                .PostAsync(
                    new() { Criteria = criteria },
                    requestConfiguration =>
                    {
                        requestConfiguration.Headers.Add("workbook-session-id", sessionId);
                    },
                    cancellationToken);

            // 4. Haal alle kolomnamen op
            var columns = await client.Drives[driveId]
                .Items[itemId]
                .Workbook
                .Tables[tableName]
                .Columns
                .GetAsync(rc => rc.Headers.Add("workbook-session-id", sessionId), cancellationToken);

            var columnNames = columns?.Value?.Select(c => c.Name).ToList() ?? [];

            // 5. Haal de gefilterde data op (zelfde als in je bestaande tool)
            var bodyRange = await client.Drives[driveId]
                .Items[itemId]
                .Workbook
                .Tables[tableName]
                .DataBodyRange
                .GetAsync(rc => rc.Headers.Add("workbook-session-id", sessionId), cancellationToken);

            var addressOnly = (bodyRange?.AddressLocal ?? bodyRange?.Address)!.Split('!').Last();

            var url =
                $"https://graph.microsoft.com/beta/drives/{driveId}/items/{itemId}" +
                $"/workbook/worksheets('{worksheetName}')" +
                $"/range(address='{addressOnly}')/visibleView" +
                "?$select=values,rowCount,columnCount,rows&$expand=rows";

            var reqInfo = new Microsoft.Kiota.Abstractions.RequestInformation
            {
                HttpMethod = Microsoft.Kiota.Abstractions.Method.GET,
                UrlTemplate = url
            };
            reqInfo.Headers.Add("workbook-session-id", sessionId);

            var view = await client.RequestAdapter.SendAsync(
                reqInfo,
                Microsoft.Graph.Beta.Models.WorkbookRangeView.CreateFromDiscriminatorValue,
                cancellationToken: cancellationToken);

            // Matrix helpers
            static List<List<object?>> ToMatrix(UntypedNode? n)
            {
                if (n == null) return new();
                var w = new Microsoft.Kiota.Serialization.Json.JsonSerializationWriterFactory()
                    .GetSerializationWriter("application/json");
                w.WriteObjectValue(null, n);
                using var s = w.GetSerializedContent();
                return System.Text.Json.JsonSerializer.Deserialize<List<List<object?>>>(s) ?? new();
            }

            // Pak alle zichtbare rijen
            var matrices = new List<List<object?>>();
            if (view?.Rows is { Count: > 0 })
            {
                foreach (var rv in view.Rows)
                    matrices.AddRange(ToMatrix(rv.Values));
            }
            else
            {
                matrices.AddRange(ToMatrix(view?.Values));
            }

            // Map naar dicts o.b.v. columnNames
            var rowObjs = matrices.Select(cells =>
            {
                var dict = new Dictionary<string, object?>(columnNames.Count);
                for (int i = 0; i < columnNames.Count; i++)
                    dict[columnNames[i]!] = i < cells.Count ? cells[i] : null;
                return dict;
            }).ToList();

            var workbookGraphUrl = $"https://graph.microsoft.com/beta/drives/{driveId}/items/{itemId}/workbook";
            return rowObjs.ToJsonContentBlock(workbookGraphUrl).ToCallToolResult();

        }
        catch (Exception e)
        {
            return e.Message.ToErrorCallToolResponse();
        }
    }


    [Description("Get filtered rows from an Excel table on OneDrive or SharePoint via Microsoft Graph, without persisting changes.")]
    [McpServerTool(Name = "GraphWorkbooks_GetFilteredRows",
        Title = "Get filtered rows from Excel table by custom query",
        ReadOnly = true, OpenWorld = false)]
    public static async Task<CallToolResult?> GraphWorkbooks_GetFilteredRows(
        string driveId,
        string itemId,
        string worksheetName,
        string tableName,
        string filterColumn,
        [Description("First filter value, e.g. '2024-07-01'")]
        string criterion1,
        IServiceProvider serviceProvider,
        RequestContext<CallToolRequestParams> requestContext,
        [Description("Optional operator between values, e.g. 'And' or 'Or'")]
        string? operatorValue = null,
        [Description("Second filter value, for ranges (e.g. '2024-07-31')")]
        string? criterion2 = null,
        CancellationToken cancellationToken = default)
    {
        var mcpServer = requestContext.Server;
        var client = await serviceProvider.GetOboGraphClient(mcpServer);

        try
        {
            // 1. Start session (persistChanges = false)
            var session = await client.Drives[driveId]
                .Items[itemId]
                .Workbook
                .CreateSession
                .PostAsync(new()
                {
                    PersistChanges = false
                }, cancellationToken: cancellationToken);

            var sessionId = session?.Id
                ?? throw new Exception("Failed to create workbook session.");

            var criteria = new Microsoft.Graph.Beta.Models.WorkbookFilterCriteria
            {
                FilterOn = "custom",
                Criterion1 = criterion1
            };

            if (!string.IsNullOrEmpty(operatorValue))
                criteria.Operator = operatorValue;
            if (!string.IsNullOrEmpty(criterion2))
                criteria.Criterion2 = criterion2;


            // 2. Apply filter
            await client.Drives[driveId]
                .Items[itemId]
                .Workbook
                .Worksheets[worksheetName]
                .Tables[tableName]
                .Columns[filterColumn]
                .Filter
                .Apply
                .PostAsync(
                    new()
                    {
                        Criteria = criteria
                    },
                    requestConfiguration =>
                    {
                        requestConfiguration.Headers.Add("workbook-session-id", sessionId);
                    },
                    cancellationToken);

            var columns = await client.Drives[driveId]
                .Items[itemId]
                .Workbook
                .Tables[tableName]
                .Columns
                .GetAsync(requestConfiguration =>
                {
                    requestConfiguration.Headers.Add("workbook-session-id", sessionId);
                }, cancellationToken);

            var columnNames = columns?.Value?.Select(c => c.Name).ToList() ?? [];
            var bodyRange = await client.Drives[driveId]
                .Items[itemId]
                .Workbook
                .Tables[tableName]
                .DataBodyRange
                .GetAsync(rc => rc.Headers.Add("workbook-session-id", sessionId), cancellationToken);

            var addressOnly = (bodyRange?.AddressLocal ?? bodyRange?.Address)!.Split('!').Last(); // "A2:D999"
            var url =
                $"https://graph.microsoft.com/beta/drives/{driveId}/items/{itemId}" +
                $"/workbook/worksheets('{worksheetName}')" +
                $"/range(address='{addressOnly}')/visibleView" +
                "?$select=values,rowCount,columnCount,rows&$expand=rows";

            var reqInfo = new Microsoft.Kiota.Abstractions.RequestInformation
            {
                HttpMethod = Microsoft.Kiota.Abstractions.Method.GET,
                UrlTemplate = url
            };
            reqInfo.Headers.Add("workbook-session-id", sessionId);

            var view = await client.RequestAdapter.SendAsync(
                reqInfo,
                Microsoft.Graph.Beta.Models.WorkbookRangeView.CreateFromDiscriminatorValue,
                cancellationToken: cancellationToken);

            // 3) UntypedNode helpers
            static List<List<object?>> ToMatrix(UntypedNode? n)
            {
                if (n == null) return new();
                var w = new Microsoft.Kiota.Serialization.Json.JsonSerializationWriterFactory()
                    .GetSerializationWriter("application/json");
                w.WriteObjectValue(null, n);
                using var s = w.GetSerializedContent();
                return System.Text.Json.JsonSerializer.Deserialize<List<List<object?>>>(s) ?? new();
            }

            // 4) Pak alle zichtbare rijen (view.Values én view.Rows[*].Values)
            var matrices = new List<List<object?>>();
            if (view?.Rows is { Count: > 0 })
            {
                foreach (var rv in view.Rows)
                    matrices.AddRange(ToMatrix(rv.Values));   // alleen rows
            }
            else
            {
                matrices.AddRange(ToMatrix(view?.Values));    // fallback
            }

            // 5) Map naar dicts o.b.v. columnNames
            var rowObjs = matrices.Select(cells =>
            {
                var dict = new Dictionary<string, object?>(columnNames.Count);
                for (int i = 0; i < columnNames.Count; i++)
                    dict[columnNames[i]!] = i < cells.Count ? cells[i] : null;
                return dict;
            }).ToList();

            var workbookGraphUrl = $"https://graph.microsoft.com/beta/drives/{driveId}/items/{itemId}/workbook";

            return rowObjs.ToJsonContentBlock(workbookGraphUrl).ToCallToolResult();

        }
        catch (Exception e)
        {
            return e.Message.ToErrorCallToolResponse();
        }
    }


    [Description("Get an Excel chart as an image from a user's OneDrive or SharePoint via Microsoft Graph.")]
    [McpServerTool(Name = "GraphUsers_GetWorkbookChart", Title = "Get Excel chart as image",
        ReadOnly = true, OpenWorld = false)]
    public static async Task<ImageContentBlock> GraphWorkbooks_GetWorkbookChart(
        string driveId,              // ID of the drive (OneDrive, SharePoint doclib)
        string itemId,               // ID of the Excel file
        string worksheetName,        // Name of the worksheet
        string chartName,            // Name or ID of the chart
        IServiceProvider serviceProvider,
        RequestContext<CallToolRequestParams> requestContext,
        CancellationToken cancellationToken = default)
    {
        var mcpServer = requestContext.Server;
        var client = await serviceProvider.GetOboGraphClient(mcpServer);
        var imageResponse = await client
            .Drives[driveId]
            .Items[itemId]
            .Workbook
            .Worksheets[worksheetName]
            .Charts[chartName]
            .Image
            .GetAsImageGetResponseAsync(cancellationToken: cancellationToken);

        return new ImageContentBlock
        {
            MimeType = "image/png",
            Data = imageResponse?.Value ?? throw new Exception("No image data returned from Graph.")
        };
    }

    [Description("Add a chart to an Excel worksheet using Microsoft Graph.")]
    [McpServerTool(Name = "GraphExcel_AddChart", Title = "Add chart to Excel worksheet",
        ReadOnly = false, OpenWorld = false)]
    public static async Task<CallToolResult?> GraphExcel_AddChart(
    IServiceProvider serviceProvider,
    RequestContext<CallToolRequestParams> requestContext,
    [Description("ID of the drive (OneDrive, SharePoint doclib).")] string driveId,
    [Description("ID of the Excel file.")] string itemId,
    [Description("Name of the worksheet.")] string worksheetName,
    [Description("The type of chart to add. Example: ColumnStacked, Pie, Line, BarClustered, etc.")] ChartType? type = null,
    [Description("The cell range for the chart source data, e.g. 'A1:B10' or 'Sheet1!A1:C20'.")] string? sourceData = null,
    [Description("How the series are organized in the source data: by rows, columns, or auto.")] ChartSeriesBy? seriesBy = null,
    CancellationToken cancellationToken = default)
    {
        var mcpServer = requestContext.Server;
        var (typed, notAccepted) = await mcpServer.TryElicit(
            new GraphAddChartRequest
            {
                Type = type ?? default,
                SourceData = sourceData ?? string.Empty,
                SeriesBy = seriesBy ?? default
            },
            cancellationToken
        );
        if (notAccepted != null) return notAccepted;
        if (typed == null) return "Invalid result".ToErrorCallToolResponse();
        var client = await serviceProvider.GetOboGraphClient(mcpServer);

        var requestBody = new Microsoft.Graph.Beta.Drives.Item.Items.Item.Workbook.Worksheets.Item.Charts.Add.AddPostRequestBody
        {
            Type = typed.Type.ToString(),
            SourceData = new UntypedString(typed.SourceData),
            SeriesBy = typed.SeriesBy.ToString()
        };

        // Example: assumes worksheet is "Sheet1" and drive/itemId known; adapt as needed
        var chart = await client
            .Drives[driveId] // TODO: parameterize if needed
            .Items[itemId]   // TODO: parameterize if needed
            .Workbook
            .Worksheets[worksheetName] // TODO: parameterize if needed
            .Charts
            .Add
            .PostAsync(requestBody, cancellationToken: cancellationToken);

        var url = $"https://graph.microsoft.com/beta/drives/{driveId}/items/{itemId}/workbook/worksheets/{worksheetName}/charts/{chart.Id}";

        return chart.ToJsonContentBlock(url).ToCallToolResult();
    }


    [Description("Please fill in the details to add a chart to an Excel worksheet.")]
    public class GraphAddChartRequest
    {
        [JsonPropertyName("type")]
        [Required]
        [Description("The type of chart to add. Example: ColumnStacked, Pie, Line, BarClustered, etc.")]
        public ChartType Type { get; set; }

        [JsonPropertyName("sourceData")]
        [Required]
        [Description("The cell range for the chart source data, e.g. 'A1:B10' or 'Sheet1!A1:C20'.")]
        public string SourceData { get; set; } = default!;

        [JsonPropertyName("seriesBy")]
        [Required]
        [Description("How the series are organized in the source data: by rows, columns, or auto.")]
        public ChartSeriesBy SeriesBy { get; set; }
    }

    public enum ChartType
    {
        [Description("Column Stacked")]
        ColumnStacked,
        [Description("Column Clustered")]
        ColumnClustered,
        [Description("Column 100% Stacked")]
        ColumnStacked100,
        [Description("Line")]
        Line,
        [Description("Line Stacked")]
        LineStacked,
        [Description("Line 100% Stacked")]
        LineStacked100,
        [Description("Pie")]
        Pie,
        [Description("Bar Clustered")]
        BarClustered,
        [Description("Bar Stacked")]
        BarStacked,
        [Description("Bar 100% Stacked")]
        BarStacked100,
        [Description("Area")]
        Area,
        [Description("Area Stacked")]
        AreaStacked,
        [Description("Area 100% Stacked")]
        AreaStacked100,
        [Description("XY Scatter")]
        XYScatter,
        [Description("Bubble")]
        Bubble,
        // Voeg eventueel meer types toe indien nodig.
    }

    public enum ChartSeriesBy
    {
        [Description("Auto-detect series by rows or columns")]
        Auto,
        [Description("Series are organized by columns")]
        Columns,
        [Description("Series are organized by rows")]
        Rows
    }


}
