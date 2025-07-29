using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text;
using System.Text.Json;
using CsvHelper;
using CsvHelper.Configuration;
using MCPhappey.Common.Extensions;
using MCPhappey.Common.Models;
using MCPhappey.Core.Extensions;
using MCPhappey.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Graph.Beta.Models;
using Microsoft.Kiota.Abstractions.Serialization;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace MCPhappey.Tools.OpenAI.Analysis;

public static class OpenAIAnalysis
{
    [Description("Execute a compact but powerful analytics query with filtering, grouping, aggregates, sorting and projection on a CSV file.")]
    [McpServerTool(
        Name = "OpenAI_ExecuteCSVDataQuery",
        Title = "Run CSV analytics query",
        ReadOnly = true)]
    public static async Task<CallToolResult> OpenAI_ExecuteCSVDataQuery(
        [Description("OneDrive/SharePoint sharing URL of the  CSV file")]
        string csvFileUrl,

        IServiceProvider serviceProvider,
        RequestContext<CallToolRequestParams> requestContext,

        [Description("Filter JSON using operators like $eq, $eqi, $in, $gt, $lte, $regex, $or, $and. Example: {\"Status\":{\"$eqi\":\"Open\"},\"Amount\":{\"$gt\":1000}}")]
        [MaxLength(20000)]
        string? filter = null,

        [Description("Group-by columns. Use \"~Col\" to indicate case-insensitive grouping (toLower). Example: [\"~Customer\",\"Region\"].")]
        string[]? groupBy = null,

        [Description("Aggregates in alias:op[:column] form. Ops: count, countDistinct, sum, avg, min, max. Use '-' when op needs no column (e.g. count). Example: [\"orders:count:-\",\"revenue:sum:Amount\"]. Ignored when aggregateJson is provided.")]
        string[]? aggregates = null,

        [Description("Advanced alias-first aggregate JSON. When provided, this overrides 'aggregates'. Example: {\"orders\":{\"$count\":true},\"revPerOrder\":{\"$divide\":[{\"$sum\":\"Amount\"},{\"$count\":true}]}}")]
        [MaxLength(40000)]
        string? aggregateJson = null,

        [Description("Sort directives. Each item 'field:asc' | 'field:desc' | 'field:1' | 'field:-1'. You may sort by group keys or aggregate aliases.")]
        string[]? sort = null,

        [Description("Row limit AFTER sorting.")]
        int? limit = null,

        [Description("Top-K per group as 'metricAlias:order:k'. Order 'asc'|'desc'. Applies only when grouping. Example: 'revenue:desc:3'.")]
        string? topK = null,

        [Description("Final fields to include (group keys and/or aggregate aliases). Keeps output compact.")]
        string[]? selectInclude  = null,

        [Description("Rename map as JSON object {\"from\":\"to\"}. Applied after include. Example: {\"revenue\":\"totalRevenue\"}.")]
        [MaxLength(20000)]
        string? selectRename  = null,

        [Description("Drop null/empty values from final objects.")]
        bool? selectExcludeNulls  = null,

        [Description("Output style. Omit or 'flat' for normal; use 'nested' to place aggregates under 'metrics'.")]
        string? outputNaming  = null,

        [Description("Set to false to hide group keys for global aggregates. Default is true.")]
        bool? includeGroupKeys  = null,

        [Description("If true, collapse/omit many null fields for smaller payloads.")]
        bool? compactNulls  = null,

        [Description("When returning raw filtered rows, restrict to these columns. Prefer this instead of 'selectInclude' for very simple extractions.")]
        string[]? limitFields = null,

        [Description("Optional per-row compute spec JSON")]
        string? compute = null,
        [Description("Optional having filter (post‑aggregate) JSON")]
        string? having = null,
        [Description("Optional select expressions JSON")]
        string? selectExpressions = null,
        CancellationToken cancellationToken = default)
    {

        try
        {
            var downloadService = serviceProvider.GetRequiredService<DownloadService>();
            var csvRawFiles = await downloadService.ScrapeContentAsync(
                serviceProvider, requestContext.Server, csvFileUrl, cancellationToken);

            var csvRaw = csvRawFiles
                .FirstOrDefault(a => a.MimeType?.Equals("text/csv", StringComparison.OrdinalIgnoreCase) == true)
                ?.Contents?.ToString();

            if (string.IsNullOrWhiteSpace(csvRaw))
                return "CSV file empty or not found.".ToErrorCallToolResponse();

            var table = LoadCsvToGenericTable(csvRaw);

            var specJson = BuildCanonicalQuerySpecJson(
                filter,
                groupBy,
                aggregates,
                aggregateJson,
                sort,
                limit,
                topK,
                selectInclude,
                selectRename,
                selectExcludeNulls,
                outputNaming,
                includeGroupKeys,
                compactNulls,
                limitFields,
                compute,
                having,
                selectExpressions);

            using var doc = JsonDocument.Parse(specJson);
            var rows = DataQueryExtenions.ExecuteGenericQuery(table, doc.RootElement);

            var payload = new
            {
                spec = JsonDocument.Parse(specJson).RootElement, // for transparency
                rowCount = rows.Count,
                rows
            };

            return payload.ToJsonContentBlock(csvFileUrl).ToCallToolResult();
        }
        catch (Exception e)
        {
            return (e.Message).ToErrorCallToolResponse();
        }
        // TODO: Parse parameters into internal QuerySpec, execute, and return results.
        //  throw new NotImplementedException("Data_Query_Run not implemented yet.");
    }

    [Description("Execute a compact but powerful analytics query with filtering, grouping, aggregates, sorting and projection on a Excel file.")]
    [McpServerTool(
        Name = "OpenAI_ExecuteExcelDataQuery",
        Title = "Run Excel analytics query",
        ReadOnly = true)]
    public static async Task<CallToolResult> OpenAI_ExecuteExcelDataQuery(
        string excelFileUrl,
        string tableName,
        IServiceProvider serviceProvider,
        RequestContext<CallToolRequestParams> requestContext,
        string? filter = null,
        string[]? groupBy = null,
        string[]? aggregates = null,
        string? aggregateJson = null,
        string[]? sort = null,
        int? limit = null,
        string? topK = null,
        string[]? selectInclude = null,
        string? selectRename = null,
        bool? selectExcludeNulls = null,
        string? outputNaming = null,
        bool? includeGroupKeys = null,
        bool? compactNulls = null,
        string[]? limitFields = null,
        string? compute = null,
        string? having = null,
        string? selectExpressions = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(excelFileUrl);
        ArgumentNullException.ThrowIfNullOrWhiteSpace(tableName);

        try
        {
            var graphClient = await serviceProvider.GetOboGraphClient(requestContext.Server);
            var driveItem = await graphClient.GetDriveItem(excelFileUrl, cancellationToken);
            if (driveItem is null)
                return "Could not resolve DriveItem from the provided URL.".ToErrorCallToolResponse();

            var table = await ExcelToGenericTable(driveItem, tableName, serviceProvider, requestContext, cancellationToken);
            if (table is null)
                return "Failed to read Excel table.".ToErrorCallToolResponse();

            var specJson = BuildCanonicalQuerySpecJson(
                filter,
                groupBy,
                aggregates,
                aggregateJson,
                sort,
                limit,
                topK,
                selectInclude,
                selectRename,
                selectExcludeNulls,
                outputNaming,
                includeGroupKeys,
                compactNulls,
                limitFields,
                compute,
                having,
                selectExpressions);

            using var doc = JsonDocument.Parse(specJson);
            var rows = DataQueryExtenions.ExecuteGenericQuery(table, doc.RootElement);

            var payload = new
            {
                spec = JsonDocument.Parse(specJson).RootElement,
                rowCount = rows.Count,
                rows
            };

            return payload.ToJsonContentBlock(excelFileUrl).ToCallToolResult();

        }
        catch (Exception e)
        {
            return (e.Message + e.StackTrace).ToErrorCallToolResponse();
        }

    }

    [Description("Get a preview of data from a specific worksheet or table in an Excel file. Returns columns, row count, and sample rows for AI analysis.")]
    [McpServerTool(
     Name = "OpenAIAnalysis_GetExcelDataSample",
     Title = "Get Excel data sample",
     ReadOnly = true)]
    public static async Task<CallToolResult?> OpenAIAnalysis_GetExcelDataSample(
              [Description("OneDrive/SharePoint sharing URL of the Excel file")]
             string excelFileUrl,
              [Description("Name of the table")]
             string tableName,
              IServiceProvider serviceProvider,
              RequestContext<CallToolRequestParams> requestContext,
              [Description("Number of data rows")]
             int? numberOfRows = 5,
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

            var table = await ExcelToGenericTable(driveItem, tableName, serviceProvider, requestContext, cancellationToken);

            return new
            {
                table?.Columns,
                Rows = table?.Rows?.Take(numberOfRows ?? 5),
                RowCount = table?.Rows.Count
            }.ToJsonContentBlock(excelFileUrl).ToCallToolResult();

        }
        catch (Exception ex)
        {
            return ex.Message.ToErrorCallToolResponse();
        }
    }

    [Description("Get a preview of data from a CSV file, including columns, row count, and sample rows for AI analysis.")]
    [McpServerTool(
     Name = "OpenAIAnalysis_GetCSVDataSample",
     Title = "Get CSV data sample",
     ReadOnly = true)]
    public static async Task<CallToolResult?> OpenAIAnalysis_GetCSVDataSample(
         [Description("Url of the CSV file")]
            string csvFileUrl,
         IServiceProvider serviceProvider,
         RequestContext<CallToolRequestParams> requestContext,
         [Description("Number of data rows")]
            int? numberOfRows = 5,
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
        var table = LoadCsvToGenericTable(csvRaw);

        return new
        {
            table?.Columns,
            Rows = table?.Rows?.Take(numberOfRows ?? 5),
            RowCount = table?.Rows?.Count
        }.ToJsonContentBlock(csvFileUrl)
        .ToCallToolResult();
    }

    private static bool TryParseJson(string json, out JsonDocument? doc)
    {
        try
        {
            doc = JsonDocument.Parse(json);
            return true;
        }
        catch
        {
            doc = null;
            return false;
        }
    }
    /*private static List<Dictionary<string, object?>> ExecuteWithSpec(GenericTable table, string specJson)
    {
        using var doc = JsonDocument.Parse(specJson);
        return ExecuteGenericQuery(table, doc.RootElement);
    }*/
    private static string BuildCanonicalQuerySpecJson(
        string? filterJson,
        string[]? groupBy,
        string[]? aggregates,
        string? aggregateJson,
        string[]? sort,
        int? limit,
        string? topK,
        string[]? selectInclude,
        string? selectRenameJson,
        bool? selectExcludeNulls,
        string? outputNaming,
        bool? includeGroupKeys,
        bool? compactNulls,
        string[]? limitFields,
        string? compute,
        string? having,
        string? selectExpressions)
    {
        JsonDocument? filterDoc = null;
        JsonDocument? aggDoc = null;
        JsonDocument? renameDoc = null;

        var hasFilter = !string.IsNullOrWhiteSpace(filterJson) && TryParseJson(filterJson!, out filterDoc);
        var hasAggJson = !string.IsNullOrWhiteSpace(aggregateJson) && TryParseJson(aggregateJson!, out aggDoc);
        var hasRename = !string.IsNullOrWhiteSpace(selectRenameJson) && TryParseJson(selectRenameJson!, out renameDoc);

        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = false }))
        {
            writer.WriteStartObject();

            // filter
            if (hasFilter && filterDoc is not null)
            {
                writer.WritePropertyName("filter");
                filterDoc.RootElement.WriteTo(writer);
            }

            // groupBy
            if (groupBy is { Length: > 0 })
            {
                writer.WritePropertyName("groupBy");
                writer.WriteStartArray();
                foreach (var g in groupBy)
                {
                    if (string.IsNullOrWhiteSpace(g)) continue;
                    if (g.StartsWith("~"))
                    {
                        var col = g[1..].Trim();
                        if (col.Length == 0) continue;
                        writer.WriteStartObject();
                        writer.WriteString("$toLower", col);
                        writer.WriteEndObject();
                    }
                    else
                    {
                        writer.WriteStringValue(g);
                    }
                }
                writer.WriteEndArray();
            }

            // aggregate
            if (hasAggJson && aggDoc is not null)
            {
                writer.WritePropertyName("aggregate");
                aggDoc.RootElement.WriteTo(writer);
            }
            else if (aggregates is { Length: > 0 })
            {
                writer.WritePropertyName("aggregate");
                writer.WriteStartObject();
                foreach (var item in aggregates)
                {
                    if (string.IsNullOrWhiteSpace(item)) continue;
                    var parts = item.Split(':', 3, StringSplitOptions.TrimEntries);
                    if (parts.Length < 2) continue;

                    var alias = parts[0];
                    var op = parts[1].ToLowerInvariant();
                    var col = parts.Length >= 3 ? parts[2] : null;

                    writer.WritePropertyName(alias);
                    writer.WriteStartObject();
                    switch (op)
                    {
                        case "count":
                            writer.WriteBoolean("$count", true);
                            break;
                        case "countdistinct":
                            if (!string.IsNullOrWhiteSpace(col)) writer.WriteString("$countDistinct", col!);
                            break;
                        case "sum":
                            if (!string.IsNullOrWhiteSpace(col)) writer.WriteString("$sum", col!);
                            break;
                        case "avg":
                            if (!string.IsNullOrWhiteSpace(col)) writer.WriteString("$avg", col!);
                            break;
                        case "min":
                            if (!string.IsNullOrWhiteSpace(col)) writer.WriteString("$min", col!);
                            break;
                        case "max":
                            if (!string.IsNullOrWhiteSpace(col)) writer.WriteString("$max", col!);
                            break;
                        default:
                            // ignore unknown op
                            break;
                    }
                    writer.WriteEndObject();
                }
                writer.WriteEndObject();
            }

            // sort
            if (sort is { Length: > 0 })
            {
                writer.WritePropertyName("sort");
                writer.WriteStartObject();
                foreach (var s in sort)
                {
                    if (string.IsNullOrWhiteSpace(s)) continue;
                    var parts = s.Split(':', 2, StringSplitOptions.TrimEntries);
                    var field = parts[0];
                    var dir = parts.Length == 2 ? parts[1] : "asc";

                    if (dir.Equals("1") || dir.Equals("+1"))
                        writer.WriteNumber(field, 1);
                    else if (dir.Equals("-1"))
                        writer.WriteNumber(field, -1);
                    else if (dir.Equals("asc", StringComparison.OrdinalIgnoreCase) ||
                             dir.Equals("desc", StringComparison.OrdinalIgnoreCase))
                        writer.WriteString(field, dir.ToLowerInvariant());
                    else
                        writer.WriteString(field, dir);
                }
                writer.WriteEndObject();
            }

            // limit
            if (limit is > 0)
                writer.WriteNumber("limit", limit.Value);

            // topKPerGroup
            if (!string.IsNullOrWhiteSpace(topK))
            {
                var parts = topK.Split(':', 3, StringSplitOptions.TrimEntries);
                if (parts.Length >= 1)
                {
                    var by = parts[0];
                    var order = (parts.Length >= 2 && parts[1].Equals("asc", StringComparison.OrdinalIgnoreCase)) ? "asc" : "desc";
                    var kVal = 3;
                    if (parts.Length >= 3 && int.TryParse(parts[2], out var kParsed) && kParsed > 0) kVal = kParsed;

                    writer.WritePropertyName("topKPerGroup");
                    writer.WriteStartObject();
                    writer.WriteString("by", by);
                    writer.WriteString("order", order);
                    writer.WriteNumber("k", kVal);
                    writer.WriteEndObject();
                }
            }

            // select
            var hasSelectInclude = selectInclude is { Length: > 0 };
            var hasSelectExcludeNulls = selectExcludeNulls == true;

            if (hasSelectInclude || hasRename || hasSelectExcludeNulls)
            {
                writer.WritePropertyName("select");
                writer.WriteStartObject();

                if (hasSelectInclude)
                {
                    writer.WritePropertyName("include");
                    writer.WriteStartArray();
                    foreach (var f in selectInclude!)
                        if (!string.IsNullOrWhiteSpace(f)) writer.WriteStringValue(f);
                    writer.WriteEndArray();
                }

                if (hasRename && renameDoc is not null && renameDoc.RootElement.ValueKind == JsonValueKind.Object)
                {
                    writer.WritePropertyName("rename");
                    renameDoc.RootElement.WriteTo(writer);
                }

                if (hasSelectExcludeNulls)
                    writer.WriteBoolean("excludeNulls", true);

                writer.WriteEndObject();
            }

            // outputNaming (emit only "nested")
            if (!string.IsNullOrWhiteSpace(outputNaming) &&
                outputNaming.Equals("nested", StringComparison.OrdinalIgnoreCase))
            {
                writer.WriteString("outputNaming", "nested");
            }

            // includeGroupKeys (only emit when false)
            if (includeGroupKeys == false)
                writer.WriteBoolean("includeGroupKeys", false);

            // compactNulls
            if (compactNulls == true)
                writer.WriteBoolean("compactNulls", true);

            // limitFields
            if (limitFields is { Length: > 0 })
            {
                writer.WritePropertyName("limitFields");
                writer.WriteStartArray();
                foreach (var f in limitFields)
                    if (!string.IsNullOrWhiteSpace(f)) writer.WriteStringValue(f);
                writer.WriteEndArray();
            }

            if (!string.IsNullOrWhiteSpace(compute) && TryParseJson(compute, out var computeDoc))
            {
                writer.WritePropertyName("compute");
                computeDoc!.RootElement.WriteTo(writer);
            }

            if (!string.IsNullOrWhiteSpace(having) && TryParseJson(having, out var havingDoc))
            {
                writer.WritePropertyName("having");
                havingDoc!.RootElement.WriteTo(writer);
            }

            if (!string.IsNullOrWhiteSpace(selectExpressions) &&
                TryParseJson(selectExpressions, out var exprDoc))
            {
                writer.WritePropertyName("expressions");
                exprDoc!.RootElement.WriteTo(writer);
            }

            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static GenericTable LoadCsvToGenericTable(string csvRaw)
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

        var table = new GenericTable();
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



    private static async Task<GenericTable?> ExcelToGenericTable(
        //        [Description("OneDrive/SharePoint sharing URL of the Excel file")]
        //           string excelFileUrl,

        DriveItem driveItem,
                //   [Description("Name of the table")]
                string tableName,
        IServiceProvider serviceProvider,
        RequestContext<CallToolRequestParams> requestContext,
        CancellationToken cancellationToken = default)
    {
        var graphClient = await serviceProvider.GetOboGraphClient(requestContext.Server);
        //  var downloadService = serviceProvider.GetService<DownloadService>();
        //  var driveItem = await graphClient.GetDriveItem(excelFileUrl, cancellationToken);
        //  if (driveItem is null)
        //      return null;

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
            return null;

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
                return null;

            var bodyMatrix = UntypedToMatrix(bodyRange.Values);
            if (bodyMatrix.Count == 0)
                return null;

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

        return tableModel;
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

}
