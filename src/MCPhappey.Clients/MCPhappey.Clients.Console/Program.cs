

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using MCPhappey.Console;
using MCPhappey.Common.Models;
using Microsoft.Extensions.Configuration;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol.Transport;

var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";

var config = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true)
    .Build();

var settings = config.Get<AppSettings>();

using var httpClient = new HttpClient();
var items = await httpClient.GetFromJsonAsync<MCPServerList>(settings?.MCPServer);

var mcpServers = settings?.Servers == null || settings?.Servers?.Any() == false ?
    items?.Servers : items?.Servers.Where(a => settings!.Servers.Contains(a.Key));

McpClientOptions clientOptions = new()
{
    ClientInfo = new()
    {
        Name = "TestClient",
        Version = "1.0.0",
    },
    Capabilities = new()
    {
        Sampling = !string.IsNullOrEmpty(settings?.OpenAI_ApiKey) ? new ModelContextProtocol.Protocol.Types.SamplingCapability()
        {
            SamplingHandler = async (request, progress, cancellationToken) =>
            {
                var chatClient = new OpenAI.OpenAIClient(settings?.OpenAI_ApiKey)
                    .GetChatClient(request?.ModelPreferences?.Hints?.First().Name
                    ?? "gpt4-mini");

                var textItems = request?.Messages.Where(a => !string.IsNullOrEmpty(a.Content.Text));

                IEnumerable<OpenAI.Chat.ChatMessage> messages =
             Enumerable.Select<ModelContextProtocol.Protocol.Types.SamplingMessage, OpenAI.Chat.ChatMessage>(
                 textItems ?? Enumerable.Empty<ModelContextProtocol.Protocol.Types.SamplingMessage>(),
                 a => a.Role == ModelContextProtocol.Protocol.Types.Role.Assistant
                     ? OpenAI.Chat.ChatMessage.CreateAssistantMessage(a.Content.Text)
                     : OpenAI.Chat.ChatMessage.CreateUserMessage(a.Content.Text)
             );

                var completion = await chatClient.CompleteChatAsync(messages, cancellationToken: cancellationToken);

                return new ModelContextProtocol.Protocol.Types.CreateMessageResult()
                {
                    Content = new()
                    {
                        Text = string.Join("\n\n", completion.Value.Content.Select(z => z.Text))
                    },
                    Model = completion.Value?.Model!,
                    Role = ModelContextProtocol.Protocol.Types.Role.Assistant
                };
            }
        } : null
    }
};

ConsoleWriter.WriteHeader($"Servers found: {mcpServers?.Count() ?? 0}");

int totalTools = 0;
int totalResources = 0;
int totalResourceTemplates = 0;
int totalPrompts = 0;

foreach (var item in mcpServers ?? [])
{
    ConsoleWriter.WriteSection($"Connecting to: {item.Key}", ConsoleColor.Green);
    ConsoleWriter.WriteSection($"Url: {item.Value.Url}", ConsoleColor.White);

    var transportOptions = new SseClientTransportOptions
    {
        Endpoint = new Uri(item.Value.Url),   // URL of the MCP “/mcp” endpoint
        UseStreamableHttp = true,                      // <-- enable the new transport
    };

    IClientTransport clientTransport = new SseClientTransport(transportOptions);

    try
    {
        IMcpClient client;
        try
        {
            client = await McpClientFactory.CreateAsync(clientTransport, clientOptions);

            ConsoleWriter.WriteInColor($"✔ Connected to server: {item.Key}", ConsoleColor.Green);
        }

        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
        {
            ConsoleWriter.WriteInColor($"⚠ Unauthorized — trying OAuth for {item.Key}", ConsoleColor.Yellow);
            var clientItem = await OAuthHandlerAsync.Connect(
                     item.Value, clientOptions, httpClient);

            if (clientItem != null)
            {
                client = clientItem;
            }
            else
            {
                continue;
            }

            ConsoleWriter.WriteInColor($"✔ Reconnected to server with access token: {item.Key}", ConsoleColor.Green);
        }

        if (client.ServerCapabilities.Tools != null)
        {
            var tools = await client.ListToolsAsync();

            ConsoleWriter.WriteSection($"Tools ({tools.Count})");

            foreach (var tool in tools)
            {
                ConsoleWriter.WriteIndented($"🔧 {tool.Name}");

                if (settings!.ExtendedTest.HasValue && settings?.ExtendedTest.Value == true)
                {
                    if ((settings!.ToolCalls!.ContainsKey(item.Key)
                         && settings!.ToolCalls![item.Key].ContainsKey(tool.Name))
                         || IsRequiredArrayEmpty(tool.JsonSchema))
                    {
                        var result = IsRequiredArrayEmpty(tool.JsonSchema) ? [] :
                            settings!.ToolCalls![item.Key][tool.Name]
                            .ToDictionary(a => a.Key, a => (object)a.Value.ToString());

                        var toolResult = await client.CallToolAsync(tool.Name, result!);

                        ConsoleWriter.WriteIndented($"Result: {JsonSerializer.Serialize(toolResult)}");
                    }
                }
            }

            totalTools += tools.Count;
        }

        if (client.ServerCapabilities.Resources != null)
        {
            var resources = await client.ListResourcesAsync();
            ConsoleWriter.WriteSection($"Resources ({resources.Count})");

            foreach (var resource in resources)
            {
                ConsoleWriter.WriteIndented($"📄 {resource.Name} [{resource.Uri}]");

                if (settings!.ExtendedTest.HasValue && settings?.ExtendedTest.Value == true)
                {
                    var result = await client.ReadResourceAsync(resource.Uri);
                    ConsoleWriter.WriteSize(result);
                }
            }

            totalResources += resources.Count;

            var resourceTemplates = await client.ListResourceTemplatesAsync();
            ConsoleWriter.WriteSection($"Resources Templates ({resourceTemplates.Count})");

            foreach (var resourceTemplate in resourceTemplates)
            {
                ConsoleWriter.WriteIndented($"📄 {resourceTemplate.Name} [{resourceTemplate.UriTemplate}]");
            }

            totalResourceTemplates += resourceTemplates.Count;
        }

        if (client.ServerCapabilities.Prompts != null)
        {
            var prompts = await client.ListPromptsAsync();
            ConsoleWriter.WriteSection($"Prompts ({prompts.Count})");

            foreach (var prompt in prompts)
            {
                ConsoleWriter.WriteIndented($"💡 {prompt.Name}");

                if (settings!.ExtendedTest.HasValue && settings?.ExtendedTest.Value == true)
                {
                    var promptResult = await client.GetPromptAsync(prompt.Name);
                    ConsoleWriter.WriteSize(promptResult);
                }
            }

            totalPrompts += prompts.Count;
        }

        await client.DisposeAsync();
    }
    catch (Exception e)
    {
        ConsoleWriter.WriteInColor($"✖ Server error ({item.Key}): {e.Message}", ConsoleColor.Red);
    }

    Console.WriteLine(new string('-', 40));
}

ConsoleWriter.WriteHeader("Totals");
ConsoleWriter.WriteSection($"Servers: ({mcpServers?.Count()})");
ConsoleWriter.WriteSection($"Tools: ({totalTools})");
ConsoleWriter.WriteSection($"Resources: ({totalResources})");
ConsoleWriter.WriteSection($"Resource Templates: ({totalResourceTemplates})");
ConsoleWriter.WriteSection($"Prompts: ({totalPrompts})");

static bool IsRequiredArrayEmpty(JsonElement jsonElement)
{
    if (jsonElement.TryGetProperty("required", out JsonElement requiredElement) &&
        requiredElement.ValueKind == JsonValueKind.Array)
    {
        return requiredElement.GetArrayLength() == 0;
    }

    // If "required" doesn't exist or isn't an array, treat it as empty
    return true;
}
