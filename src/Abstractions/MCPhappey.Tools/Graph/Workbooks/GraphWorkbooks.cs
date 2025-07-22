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
    [Description("Get an Excel chart as an image from a user's OneDrive or SharePoint via Microsoft Graph.")]
    [McpServerTool(Name = "GraphUsers_GetWorkbookChart", ReadOnly = true, OpenWorld = false)]
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

        // Example Graph call:
        // GET /drives/{drive-id}/items/{item-id}/workbook/worksheets/{worksheet-name}/charts/{chart-name}/image

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
    [McpServerTool(Name = "GraphExcel_AddChart", ReadOnly = false, OpenWorld = false)]
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
        var (typed, notAccepted) = await mcpServer.TryElicit<GraphAddChartRequest>(
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
