
using ModelContextProtocol.Protocol;

namespace MCPhappey.Common.Extensions;

public static class SamplingExtensions
{
    public static string? ToText(this CreateMessageResult result) =>
         result.Content is TextContentBlock textContentBlock ? textContentBlock.Text : null;

    public static ModelPreferences? ToModelPreferences(this string? result) => result != null ? new()
    {
        Hints = [result.ToModelHint()!]
    } : null;

    public static ModelHint? ToModelHint(this string? result) => new() { Name = result };

}
