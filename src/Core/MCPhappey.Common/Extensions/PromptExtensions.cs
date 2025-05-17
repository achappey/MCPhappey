
using System.Text.Json;
using System.Text.RegularExpressions;

namespace MCPhappey.Core.Extensions;

public static partial class PromptExtensions
{
    public static int CountPromptArguments(this string template)
    {
        ArgumentNullException.ThrowIfNull(template);

        var matches = PromptArgumentRegex().Matches(template);
        return matches
            .Select(m => m.Groups[1].Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();
    }

    public static string FormatWith(this string template, IReadOnlyDictionary<string, JsonElement> values)
    {
        ArgumentNullException.ThrowIfNull(template);
        ArgumentNullException.ThrowIfNull(values);

        return PromptArgumentRegex().Replace(template, match =>
        {
            var key = match.Groups[1].Value;
            return values.TryGetValue(key, out var value) ? value.ToString() ?? string.Empty : match.Value;
        });
    }

    [GeneratedRegex("{(.*?)}")]
    private static partial Regex PromptArgumentRegex();

}