
using System.Text.Json;
using System.Text.RegularExpressions;

namespace MCPhappey.Core.Extensions;

public static partial class PromptExtensions
{
    public static void ValidatePrompt(
        this ModelContextProtocol.Protocol.Prompt template,
        IReadOnlyDictionary<string, JsonElement> argumentsDict)
    {
        foreach (var arg in template.Arguments ?? [])
        {
            if (arg.Required == true && !argumentsDict.ContainsKey(arg.Name))
            {
                //LoggingLevel.Alert
                throw new ArgumentException(
                    $"Missing required argument: {arg.Name}",
                    arg.Name
                );
            }
        }
    }

    public static int CountPromptArguments(this string template)
    {
        ArgumentNullException.ThrowIfNull(template);

        var matches = PromptArgumentRegex().Matches(template);

        return matches
            .Select(m => m.Groups[1].Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();
    }
    /*
        public static string FormatWith(this string template, IReadOnlyDictionary<string, JsonElement> values)
        {
            ArgumentNullException.ThrowIfNull(template);
            ArgumentNullException.ThrowIfNull(values);

            return PromptArgumentRegex().Replace(template, match =>
            {
                var key = match.Groups[1].Value;
                return values.TryGetValue(key, out var value) ? value.ToString() ?? string.Empty : match.Value;
            });
        }*/

    public static string FormatPrompt(
       this string prompt,
       ModelContextProtocol.Protocol.Prompt promptTemplate,
       IReadOnlyDictionary<string, JsonElement> argumentsDict)
    {
        promptTemplate.ValidatePrompt(argumentsDict);

        var result = PromptArgumentRegex().Replace(prompt, match =>
        {
            var argName = match.Groups[1].Value;
            var argDef = promptTemplate.Arguments?.FirstOrDefault(a => a.Name == argName);

            if (argDef == null)
                return ""; // Remove unknown placeholders

            if (argumentsDict.TryGetValue(argName, out var value))
            {
                return value.ToString();
            }

            return "";
        });

        return result;
    }


    [GeneratedRegex("{(.*?)}")]
    private static partial Regex PromptArgumentRegex();
    [GeneratedRegex(@"\s{2,}")]
    private static partial Regex CleanRegex();
}