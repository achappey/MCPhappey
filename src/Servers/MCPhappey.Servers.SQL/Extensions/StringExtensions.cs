namespace MCPhappey.Servers.SQL.Extensions;

public static class StringExtensions
{
    public static string Slugify(this string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;
        // Replace spaces with hyphens
        var replaced = input.Replace(' ', '-');
        // Remove all chars except a-z, A-Z, 0-9, and -
        return System.Text.RegularExpressions.Regex.Replace(replaced, @"[^a-zA-Z0-9\-]", "");
    }
}
