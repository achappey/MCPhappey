using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using MCPhappey.Common.Extensions;
using MCPhappey.Common.Models;
using MCPhappey.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace MCPhappey.Core.Extensions;

public static partial class DataQueryExtenions
{
    public static async Task<string?> AnalyzeDataset(this IMcpServer server,
        RequestContext<CallToolRequestParams> requestContext,
        IServiceProvider serviceProvider, GenericTable table, string analysisPrompt,
        CancellationToken cancellationToken)
    {
        var samplingService = serviceProvider.GetRequiredService<SamplingService>();
        var headers = table.Columns;
        var sampleRows = table.Rows.Take(10).ToList();
        var markdown = MarkdownTable(headers, sampleRows);

        // Prepare arguments for the analysis prompt
        var columns = string.Join(", ", headers);
        var totalRows = table.Rows.Count.ToString();
        var analysisArgs = new Dictionary<string, JsonElement>
        {
            ["columns"] = JsonSerializer.SerializeToElement(columns),
            ["totalRows"] = JsonSerializer.SerializeToElement(totalRows),
            ["sampleRows"] = JsonSerializer.SerializeToElement(markdown),
            ["analysisGoal"] = JsonSerializer.SerializeToElement(analysisPrompt)
        };

        await requestContext.Server.SendProgressNotificationAsync(
                      requestContext,
                      1,
                      "Analyzing dataset",
                      4,
                      cancellationToken);

        var analysisResult = await samplingService.GetPromptSample(
            serviceProvider, server,
            "analyze-dataset-and-suggest-queries",
            analysisArgs, "gpt-4.1-mini", cancellationToken: cancellationToken);

        var analysisText = analysisResult.ToText();

        // Extract the suggested high-level queries (as plain text, numbered list)
        // You may need to parse/clean this string if you want just the list!

        // 2. AI: Expand to executable JSON queries
        var queriesArgs = new Dictionary<string, JsonElement>
        {
            ["columns"] = JsonSerializer.SerializeToElement(columns),
            ["totalRows"] = JsonSerializer.SerializeToElement(totalRows),
            ["sampleRows"] = JsonSerializer.SerializeToElement(markdown),
            ["highLevelQueries"] = JsonSerializer.SerializeToElement(analysisText)
        };

        await requestContext.Server.SendProgressNotificationAsync(
              requestContext,
              2,
              "Creating queries",
              4,
              cancellationToken);

        var queriesResult = await samplingService.GetPromptSample(
            serviceProvider, server,
            "expand-queries-to-json-object",
            queriesArgs, "gpt-4.1", cancellationToken: cancellationToken);

        var queriesJson = queriesResult.ToText();

        // 3. Parse and execute each query
        var results = new Dictionary<string, string>();
        using var doc = JsonDocument.Parse(queriesJson?.CleanJson()!);
        var queries = doc.RootElement.GetProperty("queries").EnumerateArray().ToList();

        for (int i = 0; i < queries.Count; i++)
        {
            var querySpec = queries[i];
            //  var key = JsonSerializer.Serialize(querySpec);
            var key = $"Query {i}";
            try
            {

                var queryResult = ExecuteGenericQuery(table, querySpec);
                results[key] = JsonSerializer.Serialize(queryResult, new JsonSerializerOptions { WriteIndented = true });
            }
            catch (Exception ex)
            {
                results[key] = $"ERROR: {ex.Message}";
            }
        }

        // 6. Final summary for the user (deeper synthesis)
        var deepAnswerArgs = new Dictionary<string, JsonElement>
        {
            ["analysisGoal"] = JsonSerializer.SerializeToElement(analysisPrompt),
            ["queryResults"] = JsonSerializer.SerializeToElement(results)
        };

        await requestContext.Server.SendProgressNotificationAsync(
                   requestContext,
                    3,
                   "Creating final answer",
                   4,
                   cancellationToken);

        var deepAnswerResult = await samplingService.GetPromptSample(
            serviceProvider, server,
            "analyze-query-results-and-summarize",
            deepAnswerArgs, "gpt-4.1", cancellationToken: cancellationToken);

        await requestContext.Server.SendProgressNotificationAsync(
                        requestContext,
                        4,
                        "Finished",
                        4,
                        cancellationToken);


        return deepAnswerResult.ToText();
    }

    public static string MarkdownTable(List<string> headers, List<Dictionary<string, object?>> rows)
    {
        var sb = new StringBuilder();
        sb.AppendLine("| " + string.Join(" | ", headers) + " |");
        sb.AppendLine("|" + string.Join("|", headers.Select(_ => "---")) + "|");
        foreach (var row in rows)
            sb.AppendLine("| " + string.Join(" | ", headers.Select(h => row.TryGetValue(h, out var v) ? v?.ToString() ?? "" : "")) + " |");
        return sb.ToString();
    }

    public static List<Dictionary<string, object?>> ExecuteGenericQuery(
        GenericTable table,
        JsonElement querySpec
    )
    {
        // -------- Parse top-level spec ------------------------------------------------
        var hasFilter = querySpec.TryGetProperty("filter", out var filter);
        var hasGroupBy = querySpec.TryGetProperty("groupBy", out var gbSpec) && gbSpec.ValueKind == JsonValueKind.Array;
        var hasAggregate = querySpec.TryGetProperty("aggregate", out var aggSpec) && aggSpec.ValueKind == JsonValueKind.Object && aggSpec.EnumerateObject().Any();
        var hasSort = querySpec.TryGetProperty("sort", out var sortSpec) && sortSpec.ValueKind == JsonValueKind.Object;
        var hasLimit = querySpec.TryGetProperty("limit", out var limitSpec) && limitSpec.ValueKind == JsonValueKind.Number;

        var outputNaming = querySpec.TryGetProperty("outputNaming", out var onSpec) && onSpec.ValueKind == JsonValueKind.String
            ? onSpec.GetString() ?? "flat"
            : "flat";

        var includeGroupKeys = !(querySpec.TryGetProperty("includeGroupKeys", out var igk) && igk.ValueKind == JsonValueKind.False);

        var compactNullsRequested = querySpec.TryGetProperty("compactNulls", out var cns) && cns.ValueKind == JsonValueKind.True;

        var hasSelect = querySpec.TryGetProperty("select", out var selectSpec) && selectSpec.ValueKind == JsonValueKind.Object;

        var hasLimitFields = querySpec.TryGetProperty("limitFields", out var limitFieldsSpec) && limitFieldsSpec.ValueKind == JsonValueKind.Array;

        var hasTopKPerGroup = querySpec.TryGetProperty("topKPerGroup", out var topKSpec) && topKSpec.ValueKind == JsonValueKind.Object;

        // -------- Build groupBy list (with optional $toLower) -------------------------
        var groupByList = new List<(string field, string? op)>();
        if (hasGroupBy)
        {
            foreach (var el in gbSpec.EnumerateArray())
            {
                if (el.ValueKind == JsonValueKind.String)
                {
                    groupByList.Add((el.GetString()!, null));
                }
                else if (el.ValueKind == JsonValueKind.Object && el.TryGetProperty("$toLower", out var lowerProp))
                {
                    groupByList.Add((lowerProp.GetString()!, "$toLower"));
                }
            }
        }

        // -------- 1) Filter -----------------------------------------------------------
        IEnumerable<Dictionary<string, object?>> rows = table.Rows;
        if (hasFilter && filter.ValueKind == JsonValueKind.Object)
            rows = FilterRows(rows, filter);

        // -------- 2) Group / Aggregate ------------------------------------------------
        var results = new List<Dictionary<string, object?>>();

        if (groupByList.Count > 0)
        {
            var grouped = rows.GroupBy(
                row => groupByList.Select(gr =>
                {
                    var v = GetNestedValue(row, gr.field)?.ToString() ?? "";
                    return gr.op == "$toLower" ? v.ToLowerInvariant() : v;
                }).ToArray(),
                new SequenceComparer<string>());

            foreach (var g in grouped)
            {
                var outRow = CreateCaseInsensitiveDict();

                if (includeGroupKeys)
                {
                    for (int i = 0; i < groupByList.Count; i++)
                        outRow[groupByList[i].field] = g.Key[i];
                }

                // Aggregates
                if (hasAggregate)
                {
                    if (string.Equals(outputNaming, "nested", StringComparison.OrdinalIgnoreCase))
                    {
                        var metrics = CreateCaseInsensitiveDict();
                        ApplyAggregates(g, aggSpec, metrics);
                        outRow["metrics"] = metrics;
                    }
                    else
                    {
                        ApplyAggregates(g, aggSpec, outRow);
                    }
                }

                if (compactNullsRequested)
                    outRow = CompactRow(outRow);

                results.Add(outRow);
            }

            // 2b) Optional topKPerGroup: group aggregated results by outer keys (all but last)
            if (hasTopKPerGroup && groupByList.Count >= 2)
            {
                results = ApplyTopKPerGroup(results, groupByList, topKSpec, outputNaming);
            }
        }
        else if (hasAggregate)
        {
            // Global aggregates (no grouping)
            var outRow = CreateCaseInsensitiveDict();

            if (string.Equals(outputNaming, "nested", StringComparison.OrdinalIgnoreCase))
            {
                var metrics = CreateCaseInsensitiveDict();
                ApplyAggregates(rows, aggSpec, metrics);
                outRow["metrics"] = metrics;
            }
            else
            {
                ApplyAggregates(rows, aggSpec, outRow);
            }

            if (compactNullsRequested)
                outRow = CompactRow(outRow);

            results.Add(outRow);
        }
        else
        {
            // Raw filtered rows
            foreach (var r in rows)
                results.Add(new Dictionary<string, object?>(r, StringComparer.OrdinalIgnoreCase));

            if (hasLimitFields)
            {
                var keep = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var f in limitFieldsSpec.EnumerateArray())
                    if (f.ValueKind == JsonValueKind.String) keep.Add(f.GetString()!);

                for (int i = 0; i < results.Count; i++)
                {
                    var pr = CreateCaseInsensitiveDict();
                    foreach (var k in keep)
                        if (results[i].TryGetValue(k, out var v)) pr[k] = v;
                    results[i] = pr;
                }
            }

            if (compactNullsRequested)
                for (int i = 0; i < results.Count; i++)
                    results[i] = CompactRow(results[i]);
        }

        // -------- 3) Sort -------------------------------------------------------------
        if (hasSort)
            results = SortResults(results, sortSpec, outputNaming);

        // -------- 4) Limit ------------------------------------------------------------
        if (hasLimit)
            results = results.Take(limitSpec.GetInt32()).ToList();

        // -------- 5) Final select / projection ---------------------------------------
        if (hasSelect)
            results = ApplySelect(results, selectSpec);

        return results;
    }

    static Dictionary<string, object?> CreateCaseInsensitiveDict()
        => new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
    static List<Dictionary<string, object?>> SortResults(
        List<Dictionary<string, object?>> results,
        JsonElement sortSpec,
        string outputNaming)
    {
        IOrderedEnumerable<Dictionary<string, object?>>? ordered = null;

        foreach (var kv in sortSpec.EnumerateObject())
        {
            var field = kv.Name;
            bool desc = kv.Value.ValueKind == JsonValueKind.String
                ? string.Equals(kv.Value.GetString(), "desc", StringComparison.OrdinalIgnoreCase)
                : (kv.Value.ValueKind == JsonValueKind.Number && kv.Value.GetInt32() < 0);

            Func<Dictionary<string, object?>, IComparable?> keySelector = r =>
            {
                // Try direct
                if (r.TryGetValue(field, out var v))
                    return ToComparable(v);

                // If nested naming, look into metrics
                if (r.TryGetValue("metrics", out var m) && m is Dictionary<string, object?> md
                    && md.TryGetValue(field, out var mv))
                    return ToComparable(mv);

                // Fallback: try common suffixes (legacy)
                string[] suffixes = { "_count", "_sum", "_avg", "_min", "_max", "_countNotNull" };
                foreach (var suf in suffixes)
                {
                    if (r.TryGetValue(field + suf, out var sv))
                        return ToComparable(sv);
                }

                return ToComparable(null);
            };

            ordered = ordered == null
                ? (desc ? results.OrderByDescending(keySelector) : results.OrderBy(keySelector))
                : (desc ? ordered.ThenByDescending(keySelector) : ordered.ThenBy(keySelector));
        }

        return ordered?.ToList() ?? results;
    }
    static void ApplyAggregates(
        IEnumerable<Dictionary<string, object?>> rows,
        JsonElement aggregateSpec,
        Dictionary<string, object?> outTarget)
    {
        foreach (var prop in aggregateSpec.EnumerateObject())
        {
            var alias = prop.Name;
            var value = EvaluateAggExpression(rows, prop.Value);
            outTarget[alias] = value;
        }
    }
    static object? EvaluateAggExpression(
        IEnumerable<Dictionary<string, object?>> rows,
        JsonElement expr)
    {
        // Literals
        switch (expr.ValueKind)
        {
            case JsonValueKind.Number:
                if (expr.TryGetDecimal(out var d)) return d;
                if (expr.TryGetDouble(out var dbl)) return (decimal)dbl;
                if (expr.TryGetInt64(out var l)) return (decimal)l;
                break;
            case JsonValueKind.String:
                // treat as column reference? We will not assume; string literals are just strings.
                // If a column ref is desired, it should be wrapped in an operator like {"$sum":"Col"}.
                return expr.GetString();
            case JsonValueKind.True: return true;
            case JsonValueKind.False: return false;
            case JsonValueKind.Null: return null;
        }

        if (expr.ValueKind != JsonValueKind.Object)
            return null;

        // Operators
        if (expr.TryGetProperty("$count", out var cSpec))
            return rows.Count();

        if (expr.TryGetProperty("$countDistinct", out var cdSpec) && cdSpec.ValueKind == JsonValueKind.String)
        {
            var col = cdSpec.GetString() ?? "";
            return rows.Select(r => GetNestedValue(r, col)?.ToString() ?? "")
                       .Distinct(StringComparer.OrdinalIgnoreCase).Count();
        }

        if (expr.TryGetProperty("$sum", out var sSpec) && sSpec.ValueKind == JsonValueKind.String)
        {
            var col = sSpec.GetString() ?? "";
            return rows.Sum(r => ToDecimalSafe(GetNestedValue(r, col)));
        }

        if (expr.TryGetProperty("$avg", out var aSpec) && aSpec.ValueKind == JsonValueKind.String)
        {
            var col = aSpec.GetString() ?? "";
            var list = rows.Select(r => ToDecimalSafe(GetNestedValue(r, col))).ToList();
            return list.Count == 0 ? 0d : (double)list.Sum() / list.Count;
        }

        if (expr.TryGetProperty("$min", out var minSpec) && minSpec.ValueKind == JsonValueKind.String)
        {
            var col = minSpec.GetString() ?? "";
            return rows.Min(r => ToComparable(GetNestedValue(r, col)));
        }

        if (expr.TryGetProperty("$max", out var maxSpec) && maxSpec.ValueKind == JsonValueKind.String)
        {
            var col = maxSpec.GetString() ?? "";
            return rows.Max(r => ToComparable(GetNestedValue(r, col)));
        }

        if (expr.TryGetProperty("$divide", out var divSpec) && divSpec.ValueKind == JsonValueKind.Array)
        {
            var parts = divSpec.EnumerateArray().ToArray();
            if (parts.Length == 2)
            {
                var num = ToDecimal(EvaluateAggExpression(rows, parts[0]));
                var den = ToDecimal(EvaluateAggExpression(rows, parts[1]));
                return den == 0 ? null : (double)num / (double)den;
            }
            return null;
        }

        if (expr.TryGetProperty("$add", out var addSpec) && addSpec.ValueKind == JsonValueKind.Array)
        {
            decimal acc = 0;
            foreach (var p in addSpec.EnumerateArray())
                acc += ToDecimal(EvaluateAggExpression(rows, p));
            return acc;
        }

        if (expr.TryGetProperty("$sub", out var subSpec) && subSpec.ValueKind == JsonValueKind.Array)
        {
            var parts = subSpec.EnumerateArray().ToArray();
            if (parts.Length == 2)
            {
                var a = ToDecimal(EvaluateAggExpression(rows, parts[0]));
                var b = ToDecimal(EvaluateAggExpression(rows, parts[1]));
                return a - b;
            }
            return null;
        }

        if (expr.TryGetProperty("$mul", out var mulSpec) && mulSpec.ValueKind == JsonValueKind.Array)
        {
            decimal acc = 1;
            bool any = false;
            foreach (var p in mulSpec.EnumerateArray())
            {
                acc *= ToDecimal(EvaluateAggExpression(rows, p));
                any = true;
            }
            return any ? acc : 0;
        }

        return null;

        static decimal ToDecimal(object? v) => v is decimal dd ? dd :
            v is double ddb ? (decimal)ddb :
            v is int ii ? ii :
            v is long ll ? ll :
            v is null ? 0 :
            decimal.TryParse(v.ToString(), out var p) ? p : 0;
    }
    static List<Dictionary<string, object?>> ApplySelect(
        List<Dictionary<string, object?>> rows,
        JsonElement selectSpec)
    {
        // Parse include
        var include = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (selectSpec.TryGetProperty("include", out var inc) && inc.ValueKind == JsonValueKind.Array)
            foreach (var el in inc.EnumerateArray())
                if (el.ValueKind == JsonValueKind.String) include.Add(el.GetString()!);

        // Parse rename
        var rename = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (selectSpec.TryGetProperty("rename", out var rn) && rn.ValueKind == JsonValueKind.Object)
            foreach (var kv in rn.EnumerateObject())
                rename[kv.Name] = kv.Value.GetString() ?? "";

        // Expressions (currently only {"alias": {"$toLower": "Col"}})
        var expressions = new List<(string alias, string op, string arg)>();
        if (selectSpec.TryGetProperty("expressions", out var exprs) && exprs.ValueKind == JsonValueKind.Object)
        {
            foreach (var kv in exprs.EnumerateObject())
            {
                var alias = kv.Name;
                var obj = kv.Value;
                if (obj.ValueKind == JsonValueKind.Object && obj.TryGetProperty("$toLower", out var tl) && tl.ValueKind == JsonValueKind.String)
                    expressions.Add((alias, "$toLower", tl.GetString()!));
            }
        }

        bool excludeNulls = selectSpec.TryGetProperty("excludeNulls", out var exn) && exn.ValueKind == JsonValueKind.True;

        var output = new List<Dictionary<string, object?>>(rows.Count);
        foreach (var row in rows)
        {
            var pr = CreateCaseInsensitiveDict();

            if (include.Count == 0)
            {
                // Copy everything then rename
                foreach (var kv in row)
                    pr[rename.TryGetValue(kv.Key, out var newName) && !string.IsNullOrEmpty(newName) ? newName : kv.Key] = kv.Value;
            }
            else
            {
                foreach (var key in include)
                {
                    if (row.TryGetValue(key, out var v))
                    {
                        var outKey = rename.TryGetValue(key, out var newName) && !string.IsNullOrEmpty(newName) ? newName : key;
                        pr[outKey] = v;
                    }
                    else if (row.TryGetValue("metrics", out var m) && m is Dictionary<string, object?> md && md.TryGetValue(key, out var mv))
                    {
                        var outKey = rename.TryGetValue(key, out var newName) && !string.IsNullOrEmpty(newName) ? newName : key;
                        pr[outKey] = mv;
                    }
                }
            }

            // Apply expressions
            foreach (var (alias, op, arg) in expressions)
            {
                object? src = null;
                if (row.TryGetValue(arg, out var v1)) src = v1;
                else if (row.TryGetValue("metrics", out var m) && m is Dictionary<string, object?> md && md.TryGetValue(arg, out var mv)) src = mv;

                var sval = src?.ToString() ?? "";
                pr[alias] = op == "$toLower" ? sval.ToLowerInvariant() : sval;
            }

            if (excludeNulls)
                pr = CompactRow(pr);

            output.Add(pr);
        }

        return output;
    }
    static Dictionary<string, object?> CompactRow(Dictionary<string, object?> row)
    {
        var pr = CreateCaseInsensitiveDict();
        foreach (var kv in row)
        {
            if (kv.Value is Dictionary<string, object?> md)
            {
                var compacted = CreateCaseInsensitiveDict();
                foreach (var mv in md)
                    if (!IsNullLike(mv.Value)) compacted[mv.Key] = mv.Value;

                if (compacted.Count > 0)
                    pr[kv.Key] = compacted;
            }
            else if (!IsNullLike(kv.Value))
            {
                pr[kv.Key] = kv.Value;
            }
        }
        return pr;
    }

    static List<Dictionary<string, object?>> ApplyTopKPerGroup(
        List<Dictionary<string, object?>> aggregatedRows,
        List<(string field, string? op)> groupByList,
        JsonElement topKSpec,
        string outputNaming)
    {
        // Strategy: treat the last groupBy key as the "inner" dimension.
        // Group the aggregated rows by all group keys except the last, then keep top K
        // rows within each outer group by the requested metric alias.
        var byAlias = topKSpec.TryGetProperty("by", out var by) && by.ValueKind == JsonValueKind.String ? by.GetString()! : "";
        var orderDesc = !(topKSpec.TryGetProperty("order", out var ord) && ord.ValueKind == JsonValueKind.String && string.Equals(ord.GetString(), "asc", StringComparison.OrdinalIgnoreCase));
        var k = topKSpec.TryGetProperty("k", out var ks) && ks.ValueKind == JsonValueKind.Number ? Math.Max(1, ks.GetInt32()) : 3;

        int outerKeyCount = Math.Max(0, groupByList.Count - 1);

        var groups = aggregatedRows.GroupBy(row =>
        {
            var arr = new string[outerKeyCount];
            for (int i = 0; i < outerKeyCount; i++)
            {
                var key = groupByList[i].field;
                row.TryGetValue(key, out var v);
                arr[i] = v?.ToString() ?? "";
            }
            return arr;
        }, new SequenceComparer<string>());

        var result = new List<Dictionary<string, object?>>();

        IComparable? KeySelector(Dictionary<string, object?> r)
        {
            if (r.TryGetValue(byAlias, out var v))
                return ToComparable(v);
            if (r.TryGetValue("metrics", out var m) && m is Dictionary<string, object?> md && md.TryGetValue(byAlias, out var mv))
                return ToComparable(mv);
            return ToComparable(null);
        }

        foreach (var g in groups)
        {
            var ordered = orderDesc
                ? g.OrderByDescending(KeySelector)
                : g.OrderBy(KeySelector);

            result.AddRange(ordered.Take(k));
        }

        return result;
    }

    // ---------- Helpers ------------------------------------------------------

    // Supports flattened ("a.b") and nested dictionaries
    static object? GetNestedValue(Dictionary<string, object?> row, string dottedKey)
    {
        if (row.TryGetValue(dottedKey, out var direct))
            return direct;

        var parts = dottedKey.Split('.');
        object? current = row;
        foreach (var part in parts)
        {
            if (current is Dictionary<string, object?> dict && dict.TryGetValue(part, out var v))
            {
                current = v;
            }
            else
            {
                return null;
            }
        }
        return current;
    }

    static IEnumerable<Dictionary<string, object?>> FilterRows(
        IEnumerable<Dictionary<string, object?>> rows,
        JsonElement filterSpec)
    {
        foreach (var row in rows)
            if (RowMatchesFilter(row, filterSpec))
                yield return row;
    }

    static bool RowMatchesFilter(Dictionary<string, object?> row, JsonElement filter)
    {
        // At a given object level, all properties are ANDed together
        foreach (var prop in filter.EnumerateObject())
        {
            if (prop.NameEquals("$or"))
            {
                bool any = false;
                foreach (var clause in prop.Value.EnumerateArray())
                    if (RowMatchesFilter(row, clause)) { any = true; break; }
                if (!any) return false;           // AND with siblings
                continue;
            }

            if (prop.NameEquals("$and"))
            {
                foreach (var clause in prop.Value.EnumerateArray())
                    if (!RowMatchesFilter(row, clause)) return false;
                continue;
            }

            // Field comparison
            var field = prop.Name;
            var cell = GetNestedValue(row, field);

            if (prop.Value.ValueKind == JsonValueKind.Object)
            {
                foreach (var op in prop.Value.EnumerateObject())
                {
                    var opName = op.Name;
                    var opValObj = JsonElementToObject(op.Value);

                    switch (opName)
                    {
                        case "$eq":
                            if (!EqualsLoose(cell, opValObj)) return false;
                            break;

                        case "$eqi":
                            if (!string.Equals(cell?.ToString(), opValObj?.ToString(), StringComparison.OrdinalIgnoreCase)) return false;
                            break;

                        case "$ne":
                            if (EqualsLoose(cell, opValObj)) return false;
                            break;

                        case "$in":
                            {
                                var set = JsonArrayToHashSet(op.Value);
                                if (!set.Contains(cell?.ToString() ?? "")) return false;
                                break;
                            }

                        case "$nin":
                            {
                                var set = JsonArrayToHashSet(op.Value);
                                if (set.Contains(cell?.ToString() ?? "")) return false;
                                break;
                            }

                        case "$exists":
                            {
                                bool shouldExist = op.Value.ValueKind == JsonValueKind.True;
                                bool exists = cell != null && cell != DBNull.Value;
                                if (exists != shouldExist) return false;
                                break;
                            }

                        case "$gt":
                            if (!(Compare(cell, opValObj) > 0)) return false;
                            break;

                        case "$gte":
                            if (!(Compare(cell, opValObj) >= 0)) return false;
                            break;

                        case "$lt":
                            if (!(Compare(cell, opValObj) < 0)) return false;
                            break;

                        case "$lte":
                            if (!(Compare(cell, opValObj) <= 0)) return false;
                            break;

                        case "$regex":
                            {
                                string input = cell?.ToString() ?? "";
                                string pattern = opValObj?.ToString() ?? "";
                                bool ignoreCase = false;
                                if (prop.Value.TryGetProperty("$options", out var opt))
                                    ignoreCase = opt.GetString()?.IndexOf('i', StringComparison.OrdinalIgnoreCase) >= 0;
                                var regOpts = ignoreCase ? RegexOptions.IgnoreCase : RegexOptions.None;
                                if (!Regex.IsMatch(input, pattern, regOpts)) return false;
                                break;
                            }

                        case "$regexi":
                            {
                                string input = cell?.ToString() ?? "";
                                string pattern = opValObj?.ToString() ?? "";
                                if (!Regex.IsMatch(input, pattern, RegexOptions.IgnoreCase)) return false;
                                break;
                            }

                        // Non-standard helper: treat null / empty as falsey
                        case "$not":
                            {
                                // Interpret as: value MUST be null/empty if "$not": true ? Your original was unclear.
                                // To stay backward compatible with your prior meaning ("must be not-null"):
                                if (cell == null || cell == DBNull.Value) return false;
                                break;
                            }

                        default:
                            // Unknown op -> fail safe? We'll ignore unknown to be permissive.
                            break;
                    }
                }
            }
            else
            {
                if (!EqualsLoose(cell, JsonElementToObject(prop.Value)))
                    return false;
            }
        }

        return true;
    }

    static bool EqualsLoose(object? a, object? b)
    {
        if (IsNullLike(a) && IsNullLike(b)) return true;
        if (IsNullLike(a) || IsNullLike(b)) return false;

        // numeric compare
        if (TryToDecimal(a, out var da) && TryToDecimal(b, out var db))
            return da == db;

        return string.Equals(a?.ToString(), b?.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    static int Compare(object? a, object? b)
    {
        if (TryToDecimal(a, out var da) && TryToDecimal(b, out var db))
            return da.CompareTo(db);

        // Try DateTime
        if (DateTime.TryParse(a?.ToString(), out var ta) && DateTime.TryParse(b?.ToString(), out var tb))
            return ta.CompareTo(tb);

        return string.Compare(a?.ToString(), b?.ToString(), StringComparison.OrdinalIgnoreCase);
    }


    static decimal ToDecimalSafe(object? v)
    {
        return TryParseDecimalSmart(v, out var d) ? d : 0m;
    }

    static bool TryToDecimal(object? v, out decimal d)
    {
        return TryParseDecimalSmart(v, out d);
    }

    static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
    static readonly CultureInfo Nl = CultureInfo.GetCultureInfo("nl-NL");
    static readonly CultureInfo En = CultureInfo.GetCultureInfo("en-US"); // optional extra

    static bool TryParseDecimalSmart(object? v, out decimal d)
    {
        if (v is null) { d = 0; return false; }
        switch (v)
        {
            case decimal dd: d = dd; return true;
            case double dbl: d = (decimal)dbl; return true;
            case float fl: d = (decimal)fl; return true;
            case long l: d = l; return true;
            case int i: d = i; return true;
        }

        var s = v.ToString()?.Trim();
        if (string.IsNullOrEmpty(s)) { d = 0; return false; }

        const NumberStyles style = NumberStyles.Float | NumberStyles.AllowThousands;

        // 1) Invariant (dot decimal)
        if (decimal.TryParse(s, style, Inv, out d)) return true;

        // 2) nl-NL (comma decimal)
        if (decimal.TryParse(s, style, Nl, out d)) return true;

        // 3) en-US (similar to invariant but tolerant)
        if (decimal.TryParse(s, style, En, out d)) return true;

        // 4) Heuristic: decide decimal by last separator
        int lastComma = s.LastIndexOf(',');
        int lastDot = s.LastIndexOf('.');
        if (lastComma >= 0 || lastDot >= 0)
        {
            char decimalSep = lastComma > lastDot ? ',' : '.';
            var sb = new StringBuilder(s.Length);
            foreach (var ch in s)
            {
                if (char.IsDigit(ch)) { sb.Append(ch); continue; }
                if (ch == decimalSep)
                {
                    sb.Append('.'); // normalize decimal to '.'
                    continue;
                }
                // else: other punctuation -> drop (assume grouping or spaces)
            }
            var normalized = sb.ToString();
            if (decimal.TryParse(normalized, NumberStyles.Float, Inv, out d)) return true;
        }

        d = 0;
        return false;
    }

    static object? JsonElementToObject(JsonElement el)
    {
        return el.ValueKind switch
        {
            JsonValueKind.String => el.GetString(),
            JsonValueKind.Number => el.TryGetInt64(out var i) ? i : el.TryGetDouble(out var d) ? d : el.GetRawText(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            JsonValueKind.Array => el.GetRawText(), // handled separately if needed
            _ => el.GetRawText()
        };
    }

    static IComparable? ToComparable(object? v)
    {
        if (v == null) return null;
        if (TryToDecimal(v, out var dec)) return dec;
        if (DateTime.TryParse(v.ToString(), out var t)) return t;
        return v.ToString();
    }

    static bool IsNullLike(object? v)
    {
        if (v == null || v == DBNull.Value) return true;
        var s = v.ToString();
        if (string.IsNullOrWhiteSpace(s)) return true;
        // Treat common sentinel as null-like
        if (string.Equals(s, "0000-00-00", StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    static HashSet<string> JsonArrayToHashSet(JsonElement arr)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (arr.ValueKind != JsonValueKind.Array) return set;
        foreach (var el in arr.EnumerateArray())
            set.Add(el.ToString());
        return set;
    }

    // comparer for string[] keys in GroupBy
    sealed class SequenceComparer<T> : IEqualityComparer<T[]>
    {
        private readonly IEqualityComparer<T> _elemComparer;
        public SequenceComparer() : this(EqualityComparer<T>.Default) { }
        public SequenceComparer(IEqualityComparer<T> elemComparer) { _elemComparer = elemComparer; }

        public bool Equals(T[]? x, T[]? y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (x is null || y is null || x.Length != y.Length) return false;
            for (int i = 0; i < x.Length; i++)
                if (!_elemComparer.Equals(x[i], y[i])) return false;
            return true;
        }

        public int GetHashCode(T[] obj)
        {
            unchecked
            {
                int hash = 17;
                foreach (var item in obj)
                    hash = hash * 31 + (item?.GetHashCode() ?? 0);
                return hash;
            }
        }
    }

}