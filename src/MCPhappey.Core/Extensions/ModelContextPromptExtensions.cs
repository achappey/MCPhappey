
using System.Text.Json;
using System.Text.RegularExpressions;
using MCPhappey.Common.Models;
using MCPhappey.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Protocol.Types;

namespace MCPhappey.Core.Extensions;

public static partial class ModelContextPromptExtensions
{
    public static PromptsCapability? ToPromptsCapability(this ServerConfig serverConfig, string? authToken = null)
        => serverConfig.Server.Capabilities.Prompts != null ?
            new PromptsCapability()
            {
                ListPromptsHandler = async (request, cancellationToken)
                    =>
                {
                    var service = request.Services!.GetRequiredService<PromptService>();

                    return await service.GetServerPrompts(serverConfig, cancellationToken);
                },
                GetPromptHandler = async (request, cancellationToken)
                    =>
                {
                    var service = request.Services!.GetRequiredService<PromptService>();

                    return await service.GetServerPrompt(serverConfig, request.Params?.Name!,
                        request.Params?.Arguments ?? new Dictionary<string, JsonElement>(),
                        authToken,
                        cancellationToken);
                }
            }
            : null;


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