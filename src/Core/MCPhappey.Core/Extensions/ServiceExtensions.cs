using System.Text.Json;
using MCPhappey.Common.Models;
using MCPhappey.Common.Constants;
using MCPhappey.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using OpenAI;
using MCPhappey.Auth.Extensions;
using MCPhappey.Common;

namespace MCPhappey.Core.Extensions;

public static class ServiceExtensions
{

    private static bool HasResult(this string? result)
             => !string.IsNullOrEmpty(result) && !result.Contains("INFO NOT FOUND", StringComparison.OrdinalIgnoreCase);

    public static void WithHeaders(this IServiceProvider serviceProvider, Dictionary<string, string>? headers)
    {
        var provider = serviceProvider?.GetService<HeaderProvider>();

        if (provider != null)
        {
            provider!.Headers = headers;
        }
    }

    public static OpenAIClient GetOpenAiClient(this IServiceProvider serviceProvider)
    {
        var domainHeaders = serviceProvider.GetService<Dictionary<string, Dictionary<string, string>>>();
        var openAIKey = domainHeaders?.ContainsKey(Hosts.OpenAI) == true
            ? domainHeaders[Hosts.OpenAI]?["Authorization"].ToString()?.GetBearerToken() : null;

        return new OpenAIClient(openAIKey);
    }

    public static ServerConfig? GetServerConfig(this IServiceProvider serviceProvider,
           IMcpServer mcpServer)
    {
        var configs = serviceProvider.GetRequiredService<IReadOnlyList<ServerConfig>>();
        return configs.GetServerConfig(mcpServer);
    }

    public static async Task<CallToolResponse> ExtractWithFacts(this IServiceProvider serviceProvider,
        IMcpServer mcpServer,
        string facts,
        string factsSourceUrl,
        string query,
        int limitSources = 5,
        CancellationToken cancellationToken = default)
    {
        var samplingService = serviceProvider.GetRequiredService<SamplingService>();
        var downloadService = serviceProvider.GetRequiredService<DownloadService>();
        var config = serviceProvider.GetServerConfig(mcpServer);

        if (config == null)
        {
            return "Server error".ToErrorCallToolResponse();
        }

        Dictionary<string, string> results = [];

        var args = new Dictionary<string, JsonElement>()
        {
            ["facts"] = JsonSerializer.SerializeToElement(facts),
            ["question"] = JsonSerializer.SerializeToElement(query)
        };

        var extractWithFactsSampleTask = samplingService.GetPromptSample(serviceProvider, mcpServer,
            "extract-with-facts", args,
            "gpt-4.1-mini", 0, cancellationToken);

        var urlArgs = new Dictionary<string, JsonElement>()
        {
            ["content"] = JsonSerializer.SerializeToElement(facts),
            ["question"] = JsonSerializer.SerializeToElement(query)
        };

        var extracUrlsFromFactsSample = await samplingService.GetPromptSample(serviceProvider,
            mcpServer, "extract-urls-with-facts", urlArgs,
                           "gpt-4.1", 0, cancellationToken);

        var urls = extracUrlsFromFactsSample?.Content.Text?
                         .Split(["\", \"", ","], StringSplitOptions.RemoveEmptyEntries)
                         .Select(a => a.Trim())
                         .Where(r => r.StartsWith("http"))
                         .Distinct()
                         .ToList()
                         .Take(limitSources) ?? [];

        var downloadSemaphore = new SemaphoreSlim(5);
        var readingTasks = new List<Task>();

        foreach (var url in urls)
        {
            await downloadSemaphore.WaitAsync(cancellationToken); // Wait for an available slot

            readingTasks.Add(Task.Run(async () =>
             {
                 try
                 {
                     var urlResults = await downloadService
                              .ScrapeContentAsync(serviceProvider, mcpServer, url,
                              cancellationToken);

                     var urlResult = string.Join("\n\n", urlResults.Select(a => a.Contents.ToString()));

                     if (string.IsNullOrEmpty(urlResult?.Trim()))
                     {
                         return;
                     }

                     var urlFactArgs = new Dictionary<string, JsonElement>()
                     {
                         ["facts"] = JsonSerializer.SerializeToElement(urlResult),
                         ["question"] = JsonSerializer.SerializeToElement(query)
                     };

                     var extractFromUrlsWithFactsSample = await samplingService.GetPromptSample(serviceProvider,
                            mcpServer, "extract-with-facts",
                            urlFactArgs,
                            "gpt-4.1-mini", 0, cancellationToken);

                     if (extractFromUrlsWithFactsSample.Content.Text.HasResult())
                     {
                         results.Add(url,
                                extractFromUrlsWithFactsSample.Content.Text ?? string.Empty);
                     }
                 }
                 finally
                 {
                     downloadSemaphore.Release(); // Release the slot
                 }

             }, cancellationToken));
        }

        await Task.WhenAll(readingTasks);

        var extractWithFactsSample = await extractWithFactsSampleTask;

        if (extractWithFactsSample.Content.Text.HasResult())
        {
            results.Add(factsSourceUrl,
                extractWithFactsSample.Content.Text ?? string.Empty);
        }

        return new()
        {
            Content = [.. results.Select(a => a.Value.ToTextResourceContent(a.Key))]
        };
    }
}