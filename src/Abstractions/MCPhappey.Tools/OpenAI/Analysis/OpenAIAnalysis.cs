using System.ComponentModel;
using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using MCPhappey.Common.Extensions;
using MCPhappey.Core.Extensions;
using MCPhappey.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Kiota.Abstractions.Serialization;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using static MCPhappey.Core.Extensions.DataQueryExtenions;

namespace MCPhappey.Tools.OpenAI.Analysis;

public static class OpenAIAnalysis
{
    [Description("Reads an Excel worksheet (headers + rows) and performs the  AI analysis.")]
    [McpServerTool(
        Name = "OpenAI_AnalyzeWorksheet",
        Title = "Analyze an Excel worksheet",
        ReadOnly = true)]
    public static async Task<CallToolResult?> OpenAI_AnalyzeWorksheet(
        [Description("OneDrive/SharePoint sharing URL of the Excel file")]
            string excelFileUrl,
        [Description("Name of the table")]
            string tableName,
        [Description("Analysis goal/prompt.")]
            string analysisPrompt,
        IServiceProvider serviceProvider,
        RequestContext<CallToolRequestParams> requestContext,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(excelFileUrl))
            return "Excel file URL is required.".ToErrorCallToolResponse();

        var graphClient = await serviceProvider.GetOboGraphClient(requestContext.Server);


        try
        {
            // Resolve DriveItem from sharing URL
            var driveItem = await graphClient.GetDriveItem(excelFileUrl, cancellationToken);
            if (driveItem is null)
                return "Could not resolve DriveItem from the provided URL.".ToErrorCallToolResponse();

            var driveId = driveItem.ParentReference!.DriveId!;
            var itemId = driveItem.Id!;

            // Enumerate tables and match by name (case-insensitive).
            var tablesResp = await graphClient
                .Drives[driveId]
                .Items[itemId]
                .Workbook
                .Tables
                .GetAsync(cancellationToken: cancellationToken);

            var table = tablesResp?.Value?.FirstOrDefault(t =>
                string.Equals(t.Name, tableName, StringComparison.OrdinalIgnoreCase));

            if (table is null)
                return $"Table '{tableName}' not found.".ToErrorCallToolResponse();

            await requestContext.Server.SendProgressNotificationAsync(
                requestContext, 2, "Fetching header and data ranges", 4, cancellationToken);

            // Header row (names)
            var headerRange = await graphClient
                .Drives[driveId]
                .Items[itemId]
                .Workbook
                .Tables[table.Id!]
                .HeaderRowRange
                .GetAsync(cancellationToken: cancellationToken);

            // Data body (rows, excludes header and totals)
            var bodyRange = await graphClient
                .Drives[driveId]
                .Items[itemId]
                .Workbook
                .Tables[table.Id!]
                .DataBodyRange
                .GetAsync(cancellationToken: cancellationToken);

            // Build headers
            var headers = new List<string>();
            if (headerRange?.Values is not null)
            {
                var headerMatrix = UntypedToMatrix(headerRange.Values);
                if (headerMatrix.Count > 0)
                    headers = headerMatrix[0].Select(h => (h ?? string.Empty).Trim()).ToList();
            }

            // Fallback if headers missing: derive from first body row
            if (headers.Count == 0)
            {
                if (bodyRange?.Values is null)
                    return "Table appears empty (no headers and no rows).".ToErrorCallToolResponse();

                var bodyMatrix = UntypedToMatrix(bodyRange.Values);
                if (bodyMatrix.Count == 0)
                    return "Table has no data rows.".ToErrorCallToolResponse();

                var colCount = bodyMatrix[0].Count;
                for (int i = 0; i < colCount; i++) headers.Add($"Column{i + 1}");
            }

            // Collect rows
            var tableModel = new GenericTable { Columns = headers };

            if (bodyRange?.Values is not null)
            {
                var bodyMatrix = UntypedToMatrix(bodyRange.Values);
                foreach (var rowVals in bodyMatrix)
                {
                    var row = new Dictionary<string, object?>(headers.Count, StringComparer.Ordinal);
                    for (int c = 0; c < headers.Count; c++)
                    {
                        var val = c < rowVals.Count ? rowVals[c] : null;
                        row[headers[c]] = val;
                    }
                    tableModel.Rows.Add(row);
                }
            }

            var analysis = await requestContext.Server.AnalyzeDataset(
                requestContext, serviceProvider, tableModel, analysisPrompt, cancellationToken);

            return (analysis ?? "No analysis result returned.").ToTextCallToolResponse();
        }
        catch (Exception ex)
        {
            return ex.Message.ToErrorCallToolResponse();
        }
    }

    /// <summary>
    /// Converts Graph SDK v5 WorkbookRange.Values (UntypedNode hierarchy) into a matrix of strings.
    /// </summary>
    private static List<List<string?>> UntypedToMatrix(UntypedNode node)
    {
        var rows = new List<List<string?>>();
        if (node is UntypedArray rowsArray)
        {
            foreach (var rowNode in rowsArray.GetValue())
            {
                var row = new List<string?>();
                if (rowNode is UntypedArray colsArray)
                {
                    foreach (var colNode in colsArray.GetValue())
                        row.Add(UntypedToString(colNode));
                }
                else
                {
                    // Single value row fallback
                    row.Add(UntypedToString(rowNode));
                }
                rows.Add(row);
            }
        }
        else
        {
            // Single row fallback
            rows.Add(new List<string?> { UntypedToString(node) });
        }
        return rows;
    }

    private static string? UntypedToString(UntypedNode? node) =>
        node switch
        {
            null => null,
            UntypedString s => s.GetValue(),
            UntypedBoolean b => b.GetValue().ToString(),
            UntypedInteger i => i.GetValue().ToString(),
            UntypedLong l => l.GetValue().ToString(),
            UntypedFloat f => f.GetValue().ToString(System.Globalization.CultureInfo.InvariantCulture),
            UntypedDouble d => d.GetValue().ToString(System.Globalization.CultureInfo.InvariantCulture),
            UntypedDecimal dec => dec.GetValue().ToString(System.Globalization.CultureInfo.InvariantCulture),
            UntypedNull => null,
            UntypedObject o => o.ToString(),   // fallback
            UntypedArray a => a.ToString(),   // fallback
            _ => node.ToString()
        };


    [Description("Uses AI to analyze a CSV file, summarize its content, and generate queryable insights and results.")]
    [McpServerTool(Name = "OpenAIAnalysis_AnalyzeCSV", Title = "Perform analysis on a csv file",
        ReadOnly = true)]
    public static async Task<CallToolResult?> OpenAIAnalysis_AnalyzeCSV(
        [Description("Url of the CSV file")]
        string csvFileUrl,
        [Description("Analysis prompt")]
        string prompt,
        IServiceProvider serviceProvider,
        RequestContext<CallToolRequestParams> requestContext,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(csvFileUrl);

        var downloadService = serviceProvider.GetRequiredService<DownloadService>();
        var csvRawFiles = await downloadService.ScrapeContentAsync(serviceProvider, requestContext.Server,
           csvFileUrl, cancellationToken);
        var csvRaw = csvRawFiles.Where(a => a.MimeType?.Equals("text/csv",
            StringComparison.OrdinalIgnoreCase) == true)
            .FirstOrDefault()?.Contents.ToString();

        if (string.IsNullOrEmpty(csvRaw))
        {
            return "CSV file empty".ToErrorCallToolResponse();
        }

        var result = await requestContext.Server.AnalyzeDataset(requestContext, serviceProvider,
            LoadCsvToGenericTable(csvRaw), prompt, cancellationToken);

        return result?.ToTextCallToolResponse();
    }

    public static Core.Extensions.DataQueryExtenions.GenericTable LoadCsvToGenericTable(string csvRaw)
    {
        using var reader = new StringReader(csvRaw);
        var delimiters = new[] { ';', ',', '\t', '|' };
        var sample = csvRaw.Split('\n').FirstOrDefault() ?? "";
        var detectedDelimiter = delimiters
            .OrderByDescending(d => sample.Count(c => c == d))
            .First();

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = detectedDelimiter.ToString(),
            Encoding = Encoding.UTF8,
            HasHeaderRecord = true,
            BadDataFound = null,
            IgnoreBlankLines = true
        };

        using var csv = new CsvReader(reader, config);

        var table = new Core.Extensions.DataQueryExtenions.GenericTable();
        csv.Read();
        csv.ReadHeader();
        table.Columns = csv.HeaderRecord?.ToList() ?? [];

        while (csv.Read())
        {
            var row = new Dictionary<string, object?>();
            foreach (var col in table.Columns)
                row[col] = csv.GetField(col);
            table.Rows.Add(row);
        }
        return table;
    }
}

