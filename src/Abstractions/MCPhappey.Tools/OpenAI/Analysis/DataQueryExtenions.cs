using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using MCPhappey.Common.Models;

namespace MCPhappey.Tools.OpenAI.Analysis;

public static partial class DataQueryExtenions
{
    private enum SortKeyType
    {
        String,
        Numeric,
        DateTime
    }

    static Dictionary<string, object?> CreateCaseInsensitiveDict()
        => new(StringComparer.OrdinalIgnoreCase);

    static List<Dictionary<string, object?>> SortResults(
     List<Dictionary<string, object?>> results,
     JsonElement sortSpec,
     string outputNaming)
    {
        // Build a deterministic sequence of (field, desc, keyType)
        var sortFields = new List<(string field, bool desc, SortKeyType type)>();

        foreach (var kv in sortSpec.EnumerateObject())
        {
            var field = kv.Name;
            bool desc = kv.Value.ValueKind == JsonValueKind.String
                ? string.Equals(kv.Value.GetString(), "desc", StringComparison.OrdinalIgnoreCase)
                : (kv.Value.ValueKind == JsonValueKind.Number && kv.Value.GetInt32() < 0);

            var type = DetectSortKeyType(results, field, outputNaming);
            sortFields.Add((field, desc, type));
        }

        IOrderedEnumerable<Dictionary<string, object?>>? ordered = null;

        foreach (var (field, desc, type) in sortFields)
        {
            switch (type)
            {
                case SortKeyType.DateTime:
                    {
                        Func<Dictionary<string, object?>, DateTime> keySelector = r =>
                        {
                            var raw = GetRawFieldValue(r, field, outputNaming);
                            if (raw == null) return DateTime.MinValue;
                            if (raw is DateTime dt) return dt;
                            if (DateTime.TryParse(raw.ToString(), out var parsed)) return parsed;
                            return DateTime.MinValue;
                        };

                        ordered = ordered == null
                            ? (desc ? results.OrderByDescending(keySelector) : results.OrderBy(keySelector))
                            : (desc ? ordered.ThenByDescending(keySelector) : ordered.ThenBy(keySelector));
                        break;
                    }

                case SortKeyType.Numeric:
                    {
                        Func<Dictionary<string, object?>, decimal> keySelector = r =>
                        {
                            var raw = GetRawFieldValue(r, field, outputNaming);
                            return TryParseDecimalSmart(raw, out var d) ? d : decimal.MinValue;
                        };

                        ordered = ordered == null
                            ? (desc ? results.OrderByDescending(keySelector) : results.OrderBy(keySelector))
                            : (desc ? ordered.ThenByDescending(keySelector) : ordered.ThenBy(keySelector));
                        break;
                    }

                default: // String
                    {
                        Func<Dictionary<string, object?>, string?> keySelector = r =>
                        {
                            var raw = GetRawFieldValue(r, field, outputNaming);
                            return raw?.ToString() ?? string.Empty;
                        };

                        ordered = ordered == null
                            ? (desc ? results.OrderByDescending(keySelector, StringComparer.OrdinalIgnoreCase)
                                    : results.OrderBy(keySelector, StringComparer.OrdinalIgnoreCase))
                            : (desc ? ordered.ThenByDescending(keySelector, StringComparer.OrdinalIgnoreCase)
                                    : ordered.ThenBy(keySelector, StringComparer.OrdinalIgnoreCase));
                        break;
                    }
            }
        }

        return ordered?.ToList() ?? results;
    }

    static List<Dictionary<string, object?>> ApplyHaving(
    List<Dictionary<string, object?>> rows,
    JsonElement havingSpec,
    string outputNaming)
    {
        var output = new List<Dictionary<string, object?>>(rows.Count);
        foreach (var row in rows)
        {
            if (RowMatchesHaving(row, havingSpec, outputNaming))
                output.Add(row);
        }
        return output;
    }

    static bool RowMatchesHaving(
        Dictionary<string, object?> row,
        JsonElement spec,
        string outputNaming)
    {
        foreach (var prop in spec.EnumerateObject())
        {
            if (prop.NameEquals("$or"))
            {
                bool any = false;
                foreach (var clause in prop.Value.EnumerateArray())
                    if (RowMatchesHaving(row, clause, outputNaming)) { any = true; break; }
                if (!any) return false;
                continue;
            }
            if (prop.NameEquals("$and"))
            {
                foreach (var clause in prop.Value.EnumerateArray())
                    if (!RowMatchesHaving(row, clause, outputNaming)) return false;
                continue;
            }
            if (prop.NameEquals("$not"))
            {
                if (RowMatchesHaving(row, prop.Value, outputNaming)) return false;
                continue;
            }

            var field = prop.Name; // kan group key of metric alias zijn
            object? cell = GetRawFieldValue(row, field, outputNaming);

            if (prop.Value.ValueKind == JsonValueKind.Object)
            {
                foreach (var op in prop.Value.EnumerateObject())
                {
                    var opName = op.Name;
                    var opValObj = JsonElementToObject(op.Value);

                    switch (opName)
                    {
                        case "$eq": if (!EqualsLoose(cell, opValObj)) return false; break;
                        case "$eqi": if (!string.Equals(cell?.ToString(), opValObj?.ToString(), StringComparison.OrdinalIgnoreCase)) return false; break;
                        case "$ne": if (EqualsLoose(cell, opValObj)) return false; break;
                        case "$gt": if (!(Compare(cell, opValObj) > 0)) return false; break;
                        case "$gte": if (!(Compare(cell, opValObj) >= 0)) return false; break;
                        case "$lt": if (!(Compare(cell, opValObj) < 0)) return false; break;
                        case "$lte": if (!(Compare(cell, opValObj) <= 0)) return false; break;
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
                        case "$regex":
                            {
                                string input = cell?.ToString() ?? "";
                                string pattern = opValObj?.ToString() ?? "";
                                bool ignoreCase = false;
                                if (prop.Value.TryGetProperty("$options", out var opt) && opt.ValueKind == JsonValueKind.String)
                                    ignoreCase = opt.GetString()!.IndexOf('i', StringComparison.OrdinalIgnoreCase) >= 0;
                                var ro = ignoreCase ? RegexOptions.IgnoreCase : RegexOptions.None;
                                if (!Regex.IsMatch(input, pattern, ro)) return false;
                                break;
                            }
                        case "$regexi":
                            {
                                string input = cell?.ToString() ?? "";
                                string pattern = opValObj?.ToString() ?? "";
                                if (!Regex.IsMatch(input, pattern, RegexOptions.IgnoreCase)) return false;
                                break;
                            }
                        default:
                            // onbekende op: laat passeren
                            break;
                    }
                }
            }
            else
            {
                if (!EqualsLoose(cell, JsonElementToObject(prop.Value))) return false;
            }
        }
        return true;
    }



    static SortKeyType DetectSortKeyType(
        List<Dictionary<string, object?>> rows,
        string field,
        string outputNaming,
        int sampleSize = 200)
    {
        int seen = 0;
        int dtOk = 0;
        int numOk = 0;
        int strOk = 0;

        foreach (var r in rows)
        {
            var raw = GetRawFieldValue(r, field, outputNaming);
            if (raw == null)
            {
                strOk++; // treat null/missing as empty string candidate
            }
            else if (raw is DateTime)
            {
                dtOk++;
            }
            else
            {
                var s = raw.ToString();
                if (string.IsNullOrWhiteSpace(s))
                {
                    strOk++;
                }
                else
                {
                    if (DateTime.TryParse(s, out _))
                        dtOk++;
                    else if (TryParseDecimalSmart(s, out _))
                        numOk++;
                    else
                        strOk++;
                }
            }

            seen++;
            if (seen >= sampleSize) break;
        }

        // Prefer a type only if ALL non-empty values fit it; otherwise fall back to string
        // Count non-empty considered:
        var nonEmpty = dtOk + numOk + strOk;

        if (nonEmpty == 0) return SortKeyType.String;

        // if every non-empty parsed as DateTime
        if (dtOk > 0 && (dtOk + strOk /* strOk may be empties */) == nonEmpty && numOk == 0)
            return SortKeyType.DateTime;

        // if every non-empty parsed as Numeric
        if (numOk > 0 && (numOk + strOk) == nonEmpty && dtOk == 0)
            return SortKeyType.Numeric;

        // Mixed -> string is safest
        return SortKeyType.String;
    }

    static object? GetRawFieldValue(
        Dictionary<string, object?> r,
        string field,
        string outputNaming)
    {
        // Direct field
        if (r.TryGetValue(field, out var v))
            return v;

        // Nested metrics
        if (r.TryGetValue("metrics", out var m) && m is Dictionary<string, object?> md
            && md.TryGetValue(field, out var mv))
            return mv;

        // Legacy suffixes fallback
        string[] suffixes = { "_count", "_sum", "_avg", "_min", "_max", "_countNotNull" };
        foreach (var suf in suffixes)
        {
            if (r.TryGetValue(field + suf, out var sv))
                return sv;
        }

        return null;
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

    static object? EvaluateRowExpression(
    Dictionary<string, object?> row,
    JsonElement expr,
    JsonElement computeSpecParent)
    {
        // Literals & direct strings
        switch (expr.ValueKind)
        {
            case JsonValueKind.String:
                {
                    var key = expr.GetString()!;
                    // Treat as column ref if exists; else literal string
                    if (row.TryGetValue(key, out var v)) return v;
                    return key;
                }
            case JsonValueKind.Number:
                if (expr.TryGetDecimal(out var d)) return d;
                if (expr.TryGetDouble(out var dbl)) return (decimal)dbl;
                if (expr.TryGetInt64(out var l)) return (decimal)l;
                return expr.GetRawText();
            case JsonValueKind.True: return true;
            case JsonValueKind.False: return false;
            case JsonValueKind.Null: return null;
        }

        if (expr.ValueKind != JsonValueKind.Object) return null;

        // $toLower
        if (expr.TryGetProperty("$toLower", out var tl))
        {
            var src = EvaluateRowExpression(row, tl, computeSpecParent);
            return src?.ToString()?.ToLowerInvariant() ?? "";
        }

        // $regex / $regexi
        if (expr.TryGetProperty("$regex", out var rx) && rx.ValueKind == JsonValueKind.Array)
        {
            var args = rx.EnumerateArray().ToArray();
            if (args.Length >= 2)
            {
                var input = EvaluateRowExpression(row, args[0], computeSpecParent)?.ToString() ?? "";
                var pattern = EvaluateRowExpression(row, args[1], computeSpecParent)?.ToString() ?? "";
                var ignoreCase = false;

                if (expr.TryGetProperty("$options", out var opt) && opt.ValueKind == JsonValueKind.String)
                    ignoreCase = opt.GetString()!.IndexOf('i', StringComparison.OrdinalIgnoreCase) >= 0;

                var ro = ignoreCase ? RegexOptions.IgnoreCase : RegexOptions.None;
                return Regex.IsMatch(input, pattern, ro);
            }
            return false;
        }
        if (expr.TryGetProperty("$regexi", out var rxi) && rxi.ValueKind == JsonValueKind.Array)
        {
            var args = rxi.EnumerateArray().ToArray();
            if (args.Length >= 2)
            {
                var input = EvaluateRowExpression(row, args[0], computeSpecParent)?.ToString() ?? "";
                var pattern = EvaluateRowExpression(row, args[1], computeSpecParent)?.ToString() ?? "";
                return Regex.IsMatch(input, pattern, RegexOptions.IgnoreCase);
            }
            return false;
        }

        // $toNumber
        if (expr.TryGetProperty("$toNumber", out var tn))
        {
            var v = EvaluateRowExpression(row, tn, computeSpecParent);
            return TryParseDecimalSmart(v, out var dec) ? dec : 0m;
        }

        // $toDate
        if (expr.TryGetProperty("$toDate", out var td))
        {
            var v = EvaluateRowExpression(row, td, computeSpecParent);
            return TryToDate(v, out var dt) ? dt : null;
        }

        // $substr: [expr, start, length]
        if (expr.TryGetProperty("$substr", out var sub) && sub.ValueKind == JsonValueKind.Array)
        {
            var args = sub.EnumerateArray().ToArray();
            if (args.Length >= 2)
            {
                var s = EvaluateRowExpression(row, args[0], computeSpecParent)?.ToString() ?? "";
                var start = 0;
                var len = s.Length;

                if (args[1].ValueKind == JsonValueKind.Number)
                    start = args[1].GetInt32();
                if (args.Length >= 3 && args[2].ValueKind == JsonValueKind.Number)
                    len = args[2].GetInt32();

                if (start < 0) start = 0;
                if (len < 0) len = 0;
                if (start > s.Length) return "";
                if (start + len > s.Length) len = s.Length - start;

                return s.Substring(start, len);
            }
            return "";
        }

        // $concat: [expr, ...]
        if (expr.TryGetProperty("$concat", out var concat) && concat.ValueKind == JsonValueKind.Array)
        {
            var sb = new StringBuilder();
            foreach (var part in concat.EnumerateArray())
            {
                var v = EvaluateRowExpression(row, part, computeSpecParent);
                sb.Append(v?.ToString() ?? "");
            }
            return sb.ToString();
        }

        return null;
    }

    static IEnumerable<Dictionary<string, object?>> ApplyCompute(
        IEnumerable<Dictionary<string, object?>> rows,
        JsonElement computeSpec)
    {
        foreach (var row in rows)
        {
            // clone (case-insensitive)
            var dst = new Dictionary<string, object?>(row, StringComparer.OrdinalIgnoreCase);

            foreach (var kv in computeSpec.EnumerateObject())
            {
                var alias = kv.Name;
                var expr = kv.Value;
                var val = EvaluateRowExpression(row, expr, computeSpecParent: kv.Value);
                dst[alias] = val;
            }

            yield return dst;
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
            case JsonValueKind.String: return expr.GetString();
            case JsonValueKind.True: return true;
            case JsonValueKind.False: return false;
            case JsonValueKind.Null: return null;
        }

        if (expr.ValueKind != JsonValueKind.Object) return null;

        // Count family
        if (expr.TryGetProperty("$count", out var _)) return rows.Count();

        if (expr.TryGetProperty("$countDistinct", out var cd) && cd.ValueKind == JsonValueKind.String)
        {
            var col = cd.GetString() ?? "";
            return rows.Select(r => GetNestedValue(r, col)?.ToString() ?? "")
                       .Distinct(StringComparer.OrdinalIgnoreCase).Count();
        }

        // $countIf supports field-predicates and expression comparators
        if (expr.TryGetProperty("$countIf", out var ci) && ci.ValueKind == JsonValueKind.Object)
        {
            int c = 0;
            foreach (var r in rows)
                if (RowPredicateOrExprMatch(r, ci)) c++;
            return c;
        }

        // Column aggregates (string) OR expression aggregates (object)
        if (expr.TryGetProperty("$sum", out var sExpr))
        {
            if (sExpr.ValueKind == JsonValueKind.String)
            {
                var col = sExpr.GetString() ?? "";
                return rows.Sum(r => ToDecimalSafe(GetNestedValue(r, col)));
            }
            // expression per row
            decimal acc = 0;
            foreach (var r in rows)
                acc += ToDecimalSafe(EvaluateRowScalar(r, sExpr));
            return acc;
        }

        if (expr.TryGetProperty("$avg", out var aExpr))
        {
            if (aExpr.ValueKind == JsonValueKind.String)
            {
                var col = aExpr.GetString() ?? "";
                var list = rows.Select(r => ToDecimalSafe(GetNestedValue(r, col))).ToList();
                return list.Count == 0 ? 0d : (double)list.Sum() / list.Count;
            }
            var values = new List<decimal>();
            foreach (var r in rows)
                if (TryParseDecimalSmart(EvaluateRowScalar(r, aExpr), out var v)) values.Add(v);
            if (values.Count == 0) return 0d;
            return (double)values.Sum() / values.Count;
        }

        // $min
        if (expr.TryGetProperty("$min", out var minExpr))
        {
            // Column name
            if (minExpr.ValueKind == JsonValueKind.String)
            {
                var col = minExpr.GetString() ?? "";
                object? best = null;
                foreach (var r in rows)
                {
                    var v = GetNestedValue(r, col);
                    if (IsNullLike(v)) continue;

                    if (best == null || Compare(v, best) < 0)
                        best = v;
                }
                return best;
            }

            // Expression per row
            IComparable? bestCmp = null;
            object? bestRaw = null;
            foreach (var r in rows)
            {
                var raw = EvaluateRowScalar(r, minExpr);
                if (IsNullLike(raw)) continue;

                var cmp = ToComparable(raw);
                if (bestCmp == null || (cmp != null && cmp.CompareTo(bestCmp) < 0))
                {
                    bestCmp = cmp;
                    bestRaw = raw;
                }
            }
            return bestRaw;
        }

        // $max
        if (expr.TryGetProperty("$max", out var maxExpr))
        {
            // Column name
            if (maxExpr.ValueKind == JsonValueKind.String)
            {
                var col = maxExpr.GetString() ?? "";
                object? best = null;
                foreach (var r in rows)
                {
                    var v = GetNestedValue(r, col);
                    if (IsNullLike(v)) continue;

                    if (best == null || Compare(v, best) > 0)
                        best = v;
                }
                return best;
            }

            // Expression per row
            IComparable? bestCmp = null;
            object? bestRaw = null;
            foreach (var r in rows)
            {
                var raw = EvaluateRowScalar(r, maxExpr);
                if (IsNullLike(raw)) continue;

                var cmp = ToComparable(raw);
                if (bestCmp == null || (cmp != null && cmp.CompareTo(bestCmp) > 0))
                {
                    bestCmp = cmp;
                    bestRaw = raw;
                }
            }
            return bestRaw;
        }


        // Arithmetic (aggregate-level)
        if (expr.TryGetProperty("$divide", out var divSpec) && divSpec.ValueKind == JsonValueKind.Array)
        {
            var parts = divSpec.EnumerateArray().ToArray();
            if (parts.Length == 2)
            {
                var num = ToDec(EvaluateAggExpression(rows, parts[0]));
                var den = ToDec(EvaluateAggExpression(rows, parts[1]));
                return den == 0 ? null : (double)num / (double)den;
            }
            return null;
        }

        if (expr.TryGetProperty("$add", out var addSpec) && addSpec.ValueKind == JsonValueKind.Array)
        {
            decimal acc = 0;
            foreach (var p in addSpec.EnumerateArray())
                acc += ToDec(EvaluateAggExpression(rows, p));
            return acc;
        }

        if ((expr.TryGetProperty("$sub", out var subSpec) || expr.TryGetProperty("$subtract", out subSpec)) && subSpec.ValueKind == JsonValueKind.Array)
        {
            var parts = subSpec.EnumerateArray().ToArray();
            if (parts.Length == 2)
            {
                var a = ToDec(EvaluateAggExpression(rows, parts[0]));
                var b = ToDec(EvaluateAggExpression(rows, parts[1]));
                return a - b;
            }
            return null;
        }

        if (expr.TryGetProperty("$mul", out var mulSpec) && mulSpec.ValueKind == JsonValueKind.Array)
        {
            decimal acc = 1; bool any = false;
            foreach (var p in mulSpec.EnumerateArray())
            {
                acc *= ToDec(EvaluateAggExpression(rows, p));
                any = true;
            }
            return any ? acc : 0;
        }

        if (expr.TryGetProperty("$abs", out var absSpec))
        {
            var v = EvaluateAggExpression(rows, absSpec);
            if (TryParseDecimalSmart(v, out var dec)) return Math.Abs(dec);
            if (v is double ddb) return Math.Abs(ddb);
            if (v is int ii) return Math.Abs(ii);
            if (v is long ll) return Math.Abs(ll);
            if (decimal.TryParse(v?.ToString(), NumberStyles.Float | NumberStyles.AllowThousands, Inv, out var parsed))
                return Math.Abs(parsed);
            return 0;
        }

        // Converters usable at aggregate level
        if (expr.TryGetProperty("$toNumber", out var toNumSpec))
        {
            var v = EvaluateAggExpression(rows, toNumSpec);
            return TryParseDecimalSmart(v, out var dec) ? dec : 0m;
        }

        if (expr.TryGetProperty("$toDate", out var toDateSpec))
        {
            var v = EvaluateAggExpression(rows, toDateSpec);
            return TryToDate(v, out var dt) ? dt : null;
        }

        if (expr.TryGetProperty("$dateDiffDays", out var ddSpec) && ddSpec.ValueKind == JsonValueKind.Array)
        {
            var parts = ddSpec.EnumerateArray().ToArray();
            if (parts.Length == 2)
            {
                var aVal = EvaluateAggExpression(rows, parts[0]);
                var bVal = EvaluateAggExpression(rows, parts[1]);
                if (TryToDate(aVal, out var a) && TryToDate(bVal, out var b))
                    return (a - b).TotalDays;
            }
            return null;
        }

        return null;

        static decimal ToDec(object? v) =>
            v is decimal md ? md :
            v is double d ? (decimal)d :
            v is int i ? i :
            v is long l ? l :
            v is null ? 0m :
            decimal.TryParse(v.ToString(), NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var p) ? p : 0m;
    }

    static bool RowPredicateOrExprMatch(Dictionary<string, object?> row, JsonElement predicate)
    {
        // Support expression comparators: { "$lt": [ exprA, exprB ] } etc.
        if (predicate.ValueKind == JsonValueKind.Object)
        {
            // Single-key expression like { "$lt": [ ... ] }
            foreach (var kv in predicate.EnumerateObject())
            {
                var op = kv.Name;
                var val = kv.Value;

                if (val.ValueKind == JsonValueKind.Array &&
                    (op == "$lt" || op == "$lte" || op == "$gt" || op == "$gte" || op == "$eq" || op == "$ne"))
                {
                    var parts = val.EnumerateArray().ToArray();
                    if (parts.Length != 2) return false;
                    var a = EvaluateRowScalar(row, parts[0]);
                    var b = EvaluateRowScalar(row, parts[1]);

                    int cmp = Compare(a, b);
                    return op switch
                    {
                        "$lt" => cmp < 0,
                        "$lte" => cmp <= 0,
                        "$gt" => cmp > 0,
                        "$gte" => cmp >= 0,
                        "$eq" => EqualsLoose(a, b),
                        "$ne" => !EqualsLoose(a, b),
                        _ => false
                    };
                }

                // Otherwise, fall back to field-based RowMatchesFilter for full dialect
                return RowMatchesFilter(row, predicate);
            }
        }

        // Non-object → not supported, fallback hard-false
        return false;
    }


    static bool TryToDate(object? v, out DateTime dt)
    {
        dt = default;
        if (v is DateTime d) { dt = d; return true; }
        var s = v?.ToString();
        if (string.IsNullOrWhiteSpace(s)) return false;

        // ISO‑first
        if (DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out dt))
            return true;

        // NL
        if (DateTime.TryParse(s, Nl, DateTimeStyles.None, out dt))
            return true;

        // EN
        if (DateTime.TryParse(s, En, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out dt))
            return true;

        // Jaar‑alleen: interpreteer als 1 jan van dat jaar
        if (int.TryParse(s, out var year) && year >= 1 && year <= 9999)
        {
            dt = new DateTime(year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return true;
        }

        return false;
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
        foreach (var prop in filter.EnumerateObject())
        {
            if (prop.NameEquals("$or"))
            {
                bool any = false;
                foreach (var clause in prop.Value.EnumerateArray())
                    if (RowMatchesFilter(row, clause)) { any = true; break; }
                if (!any) return false;
                continue;
            }

            if (prop.NameEquals("$and"))
            {
                foreach (var clause in prop.Value.EnumerateArray())
                    if (!RowMatchesFilter(row, clause)) return false;
                continue;
            }

            if (prop.NameEquals("$not"))
            {
                if (RowMatchesFilter(row, prop.Value)) return false;
                continue;
            }

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
                        case "$eq": if (!EqualsLoose(cell, opValObj)) return false; break;
                        case "$eqi": if (!string.Equals(cell?.ToString(), opValObj?.ToString(), StringComparison.OrdinalIgnoreCase)) return false; break;
                        case "$ne": if (EqualsLoose(cell, opValObj)) return false; break;

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

                        case "$isTrue":
                            {
                                bool want = op.Value.ValueKind == JsonValueKind.True;
                                bool isTrue = IsTruthy(cell);
                                if (isTrue != want) return false;
                                break;
                            }
                        case "$isFalse":
                            {
                                bool want = op.Value.ValueKind == JsonValueKind.True;
                                bool isFalse = !IsTruthy(cell);
                                if (isFalse != want) return false;
                                break;
                            }

                        case "$gt": if (!(Compare(cell, opValObj) > 0)) return false; break;
                        case "$gte": if (!(Compare(cell, opValObj) >= 0)) return false; break;
                        case "$lt": if (!(Compare(cell, opValObj) < 0)) return false; break;
                        case "$lte": if (!(Compare(cell, opValObj) <= 0)) return false; break;

                        case "$regex":
                            {
                                string input = cell?.ToString() ?? "";
                                string pattern = opValObj?.ToString() ?? "";
                                bool ignoreCase = false;
                                if (prop.Value.TryGetProperty("$options", out var opt) && opt.ValueKind == JsonValueKind.String)
                                    ignoreCase = opt.GetString()!.IndexOf('i', StringComparison.OrdinalIgnoreCase) >= 0;
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

                        case "$not":
                            {
                                if (op.Value.ValueKind != JsonValueKind.Object) return false;
                                bool allPass = true;
                                foreach (var inner in op.Value.EnumerateObject())
                                {
                                    var innerOk = true;
                                    switch (inner.Name)
                                    {
                                        case "$eq": innerOk = EqualsLoose(cell, JsonElementToObject(inner.Value)); break;
                                        case "$eqi": innerOk = string.Equals(cell?.ToString(), JsonElementToObject(inner.Value)?.ToString(), StringComparison.OrdinalIgnoreCase); break;
                                        case "$ne": innerOk = !EqualsLoose(cell, JsonElementToObject(inner.Value)); break;
                                        default:
                                            // fallback: rewrap en roepen via tijdelijke filter
                                            innerOk = RowMatchesFilter(row, BuildSingleFieldFilter(field, inner));
                                            break;
                                    }
                                    if (!innerOk) { allPass = false; break; }
                                }
                                if (allPass) return false; // not(true) == false
                                break;
                            }

                        default:
                            // unknown op -> permissive
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

        static JsonElement BuildSingleFieldFilter(string field, JsonProperty innerOp)
        {
            using var doc = JsonDocument.Parse($"{{\"{field}\":{{\"{innerOp.Name}\":{innerOp.Value.GetRawText()}}}}}");
            return doc.RootElement.Clone();
        }
    }

    static bool IsTruthy(object? v)
    {
        if (v is null) return false;
        if (v is bool b) return b;
        var s = v.ToString()?.Trim().ToLowerInvariant();
        return s is "true" or "1" or "yes" or "ja";
    }

    static List<Dictionary<string, object?>> ApplyTopKPerGroup(
        List<Dictionary<string, object?>> aggregatedRows,
        List<(string field, string? op)> groupByList,
        JsonElement topKSpec,
        string outputNaming)
    {
        // Parse spec
        var byAlias = topKSpec.TryGetProperty("by", out var by) && by.ValueKind == JsonValueKind.String ? by.GetString()! : "";
        var orderDesc = !(topKSpec.TryGetProperty("order", out var ord) && ord.ValueKind == JsonValueKind.String && string.Equals(ord.GetString(), "asc", StringComparison.OrdinalIgnoreCase));
        var k = topKSpec.TryGetProperty("k", out var ks) && ks.ValueKind == JsonValueKind.Number ? Math.Max(1, ks.GetInt32()) : 3;

        // Met 1 group key is outerKeyCount = 0 (één supergroep); met n>=2 keys nemen we de eerste n-1 als outer.
        int outerKeyCount = Math.Max(0, groupByList.Count - 1);

        Func<Dictionary<string, object?>, IComparable?> keySelector = r =>
        {
            if (r.TryGetValue(byAlias, out var v))
                return ToComparable(v);
            if (r.TryGetValue("metrics", out var m) && m is Dictionary<string, object?> md && md.TryGetValue(byAlias, out var mv))
                return ToComparable(mv);
            return ToComparable(null);
        };

        IEnumerable<IGrouping<string, Dictionary<string, object?>>> groups;

        if (outerKeyCount == 0)
        {
            // Eén groep: maak één group key voor alles
            groups = aggregatedRows.GroupBy(_ => "", StringComparer.Ordinal);
        }
        else
        {
            groups = aggregatedRows.GroupBy(row =>
            {
                var sb = new StringBuilder();
                for (int i = 0; i < outerKeyCount; i++)
                {
                    var keyName = groupByList[i].field;
                    row.TryGetValue(keyName, out var v);
                    if (i > 0) sb.Append('\u001F'); // unlikely separator
                    sb.Append(v?.ToString() ?? "");
                }
                return sb.ToString();
            }, StringComparer.Ordinal);
        }

        var result = new List<Dictionary<string, object?>>();
        foreach (var g in groups)
        {
            var ordered = orderDesc
                ? g.OrderByDescending(keySelector)
                : g.OrderBy(keySelector);

            result.AddRange(ordered.Take(k));
        }

        return result;
    }
    public static List<Dictionary<string, object?>> ExecuteGenericQuery(
     GenericTable table,
     JsonElement querySpec
 )
    {
        // -------- Parse top-level spec ------------------------------------------------
        var hasCompute = querySpec.TryGetProperty("compute", out var computeSpec) && computeSpec.ValueKind == JsonValueKind.Object;

        var hasFilter = querySpec.TryGetProperty("filter", out var filter);
        var hasGroupBy = querySpec.TryGetProperty("groupBy", out var gbSpec) && gbSpec.ValueKind == JsonValueKind.Array;
        var hasAggregate = querySpec.TryGetProperty("aggregate", out var aggSpec) && aggSpec.ValueKind == JsonValueKind.Object && aggSpec.EnumerateObject().Any();
        var hasSort = querySpec.TryGetProperty("sort", out var sortSpec) && sortSpec.ValueKind == JsonValueKind.Object;
        var hasLimit = querySpec.TryGetProperty("limit", out var limitSpec) && limitSpec.ValueKind == JsonValueKind.Number;
        var hasTopKPerGroup = querySpec.TryGetProperty("topKPerGroup", out var topKSpec) && topKSpec.ValueKind == JsonValueKind.Object;
        var hasSelect = querySpec.TryGetProperty("select", out var selectSpec) && selectSpec.ValueKind == JsonValueKind.Object;
        var hasLimitFields = querySpec.TryGetProperty("limitFields", out var limitFieldsSpec) && limitFieldsSpec.ValueKind == JsonValueKind.Array;
        var hasHaving = querySpec.TryGetProperty("having", out var havingSpec) && havingSpec.ValueKind == JsonValueKind.Object;

        var outputNaming = querySpec.TryGetProperty("outputNaming", out var onSpec) && onSpec.ValueKind == JsonValueKind.String
            ? onSpec.GetString() ?? "flat"
            : "flat";

        bool includeGroupKeys = true;
        if (querySpec.TryGetProperty("includeGroupKeys", out var igk) && igk.ValueKind == JsonValueKind.False) includeGroupKeys = false;
        if (querySpec.TryGetProperty("includeGroupKeys", out igk) && igk.ValueKind == JsonValueKind.True) includeGroupKeys = true;

        var compactNullsRequested = querySpec.TryGetProperty("compactNulls", out var cns) && cns.ValueKind == JsonValueKind.True;

        // -------- Build groupBy list (with optional $toLower) -------------------------
        var groupByList = new List<(string field, string? op)>();
        if (hasGroupBy)
        {
            foreach (var el in gbSpec.EnumerateArray())
            {
                if (el.ValueKind == JsonValueKind.String)
                    groupByList.Add((el.GetString()!, null));
                else if (el.ValueKind == JsonValueKind.Object && el.TryGetProperty("$toLower", out var lowerProp))
                    groupByList.Add((lowerProp.GetString()!, "$toLower"));
            }
        }

        // -------- 0) Compute (per row) ------------------------------------------------
        IEnumerable<Dictionary<string, object?>> rows = table.Rows;
        if (hasCompute)
            rows = ApplyCompute(rows, computeSpec);

        // -------- 1) Filter -----------------------------------------------------------
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
                    for (int i = 0; i < groupByList.Count; i++)
                        outRow[groupByList[i].field] = g.Key[i];

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

            if (hasTopKPerGroup)
                results = ApplyTopKPerGroup(results, groupByList, topKSpec, outputNaming);
        }
        else if (hasAggregate)
        {
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

        // ---- HAVING (post-aggregate) -------------------------------------------------
        if (hasHaving && hasAggregate)
            results = ApplyHaving(results, havingSpec, outputNaming);

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


    static List<Dictionary<string, object?>> ApplySelect(
        List<Dictionary<string, object?>> rows,
        JsonElement selectSpec)
    {
        // include
        var include = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (selectSpec.TryGetProperty("include", out var inc) && inc.ValueKind == JsonValueKind.Array)
            foreach (var el in inc.EnumerateArray())
                if (el.ValueKind == JsonValueKind.String) include.Add(el.GetString()!);

        // rename
        var rename = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (selectSpec.TryGetProperty("rename", out var rn) && rn.ValueKind == JsonValueKind.Object)
            foreach (var kv in rn.EnumerateObject())
                rename[kv.Name] = kv.Value.GetString() ?? "";

        // expressions: support BOTH object and array forms
        var expressions = new List<(string alias, string op, string arg)>();
        if (selectSpec.TryGetProperty("expressions", out var exprs))
        {
            if (exprs.ValueKind == JsonValueKind.Object)
            {
                // { "alias": { "$toLower": "Col" }, ... }
                foreach (var kv in exprs.EnumerateObject())
                {
                    var alias = kv.Name;
                    var obj = kv.Value;
                    if (obj.ValueKind == JsonValueKind.Object && obj.TryGetProperty("$toLower", out var tl) && tl.ValueKind == JsonValueKind.String)
                        expressions.Add((alias, "$toLower", tl.GetString()!));
                }
            }
            else if (exprs.ValueKind == JsonValueKind.Array)
            {
                // [ { "alias": { "$toLower": "Col" } }, ... ]
                foreach (var item in exprs.EnumerateArray())
                {
                    if (item.ValueKind != JsonValueKind.Object) continue;
                    foreach (var kv in item.EnumerateObject())
                    {
                        var alias = kv.Name;
                        var obj = kv.Value;
                        if (obj.ValueKind == JsonValueKind.Object && obj.TryGetProperty("$toLower", out var tl) && tl.ValueKind == JsonValueKind.String)
                            expressions.Add((alias, "$toLower", tl.GetString()!));
                    }
                }
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


    static object? EvaluateRowScalar(Dictionary<string, object?> row, JsonElement expr)
    {
        switch (expr.ValueKind)
        {
            case JsonValueKind.Null: return null;
            case JsonValueKind.True: return true;
            case JsonValueKind.False: return false;
            case JsonValueKind.Number:
                if (expr.TryGetDecimal(out var dd)) return dd;
                if (expr.TryGetDouble(out var dbl)) return (decimal)dbl;
                if (expr.TryGetInt64(out var ll)) return (decimal)ll;
                return expr.GetRawText();
            case JsonValueKind.String:
                {
                    var s = expr.GetString() ?? "";
                    // Treat as column reference if present; else literal.
                    if (row.TryGetValue(s, out var v)) return v;
                    return s;
                }
        }

        if (expr.ValueKind != JsonValueKind.Object) return null;

        // Converters
        if (expr.TryGetProperty("$toNumber", out var tn))
        {
            var v = EvaluateRowScalar(row, tn);
            return TryParseDecimalSmart(v, out var dec) ? dec : 0m;
        }
        if (expr.TryGetProperty("$toDate", out var td))
        {
            var v = EvaluateRowScalar(row, td);
            return TryToDate(v, out var dt) ? dt : null;
        }

        // Arithmetic
        if (expr.TryGetProperty("$add", out var add) && add.ValueKind == JsonValueKind.Array)
        {
            decimal acc = 0;
            foreach (var p in add.EnumerateArray())
                acc += ToDec(EvaluateRowScalar(row, p));
            return acc;
        }
        if ((expr.TryGetProperty("$sub", out var sub) || expr.TryGetProperty("$subtract", out sub)) && sub.ValueKind == JsonValueKind.Array)
        {
            var parts = sub.EnumerateArray().ToArray();
            if (parts.Length == 2)
                return ToDec(EvaluateRowScalar(row, parts[0])) - ToDec(EvaluateRowScalar(row, parts[1]));
            return null;
        }
        if (expr.TryGetProperty("$mul", out var mul) && mul.ValueKind == JsonValueKind.Array)
        {
            decimal acc = 1; bool any = false;
            foreach (var p in mul.EnumerateArray())
            {
                acc *= ToDec(EvaluateRowScalar(row, p));
                any = true;
            }
            return any ? acc : 0m;
        }
        if (expr.TryGetProperty("$divide", out var div) && div.ValueKind == JsonValueKind.Array)
        {
            var parts = div.EnumerateArray().ToArray();
            if (parts.Length == 2)
            {
                var num = ToDec(EvaluateRowScalar(row, parts[0]));
                var den = ToDec(EvaluateRowScalar(row, parts[1]));
                return den == 0 ? null : (double)num / (double)den;
            }
            return null;
        }
        if (expr.TryGetProperty("$abs", out var absExpr))
        {
            var v = EvaluateRowScalar(row, absExpr);
            if (TryParseDecimalSmart(v, out var dec)) return Math.Abs(dec);
            if (v is double d) return Math.Abs(d);
            if (v is int i) return Math.Abs(i);
            if (v is long l) return Math.Abs(l);
            return 0m;
        }

        // Dates
        if (expr.TryGetProperty("$dateDiffDays", out var diff) && diff.ValueKind == JsonValueKind.Array)
        {
            var parts = diff.EnumerateArray().ToArray();
            if (parts.Length == 2)
            {
                var a = EvaluateRowScalar(row, parts[0]);
                var b = EvaluateRowScalar(row, parts[1]);
                if (TryToDate(a, out var da) && TryToDate(b, out var db))
                    return (da - db).TotalDays;
            }
            return null;
        }

        return null;

        static decimal ToDec(object? v) => v is decimal md ? md :
            v is double d ? (decimal)d :
            v is int i ? i :
            v is long l ? l :
            v is null ? 0m :
            decimal.TryParse(v.ToString(), NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var p) ? p : 0m;
    }


}