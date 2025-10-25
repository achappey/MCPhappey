using System.ComponentModel;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using MCPhappey.Common.Extensions;
using MCPhappey.Core.Extensions;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace MCPhappey.Tools.EuropeanUnion;

public static class EuropeanUnionVIESService
{
    private const string BaseUrl = "https://ec.europa.eu/taxation_customs/vies/rest-api";

    // -------------------------------------------------------
    // ✅ Validate real VAT numbers (POST)
    // -------------------------------------------------------
    [Description("Validate a real EU VAT number. Requires 2-letter country code and VAT number.")]
    [McpServerTool(
        Title = "Validate VAT number",
        Name = "european_union_vies_validate_vat",
        Idempotent = true,
        ReadOnly = true)]
    public static async Task<CallToolResult?> ValidateVat(
        [Description("Country code, e.g. 'NL'")] string countryCode,
        [Description("VAT number, e.g. '123456789B01'")] string vatNumber,
        IServiceProvider sp = null!,
        RequestContext<CallToolRequestParams> rc = null!,
        CancellationToken ct = default)
        => await ExecutePostAsync(
                sp, rc,
                "/check-vat-number",
                new JsonObject
                {
                    ["countryCode"] = countryCode,
                    ["vatNumber"] = vatNumber
                },
                ct);

    // -------------------------------------------------------
    // ✅ VIES system status (GET)
    // -------------------------------------------------------
    [Description("Retrieve operational status of VIES VAT services per Member State.")]
    [McpServerTool(
        Title = "Get VIES status",
        Name = "european_union_vies_get_status",
        Idempotent = true,
        ReadOnly = true)]
    public static async Task<CallToolResult?> GetStatus(
        IServiceProvider sp = null!,
        RequestContext<CallToolRequestParams> rc = null!,
        CancellationToken ct = default)
        => await ExecuteGetAsync(sp, rc, "/check-status", ct);

    // -------------------------------------------------------
    // ✅ Integration test validation (POST) – 100=VALID, 200=INVALID
    // -------------------------------------------------------
    [Description("Test VIES integration using EC test values. 100=valid, 200=invalid.")]
    [McpServerTool(
        Title = "Test VIES validation",
        Name = "european_union_vies_validate_test",
        Idempotent = true,
        ReadOnly = true)]
    public static async Task<CallToolResult?> ValidateTest(
        [Description("VAT number: 100 or 200")] string vatNumber,
        IServiceProvider sp = null!,
        RequestContext<CallToolRequestParams> rc = null!,
        CancellationToken ct = default)
        => await ExecutePostAsync(
                sp, rc,
                "/check-vat-test-service",
                new JsonObject
                {
                    ["countryCode"] = "NL", // mandatory placeholder
                    ["vatNumber"] = vatNumber
                },
                ct);

    // =======================================================
    // Shared HTTP executors (clean and small)
    // =======================================================
    private static async Task<CallToolResult?> ExecutePostAsync(
        IServiceProvider sp,
        RequestContext<CallToolRequestParams> rc,
        string endpoint,
        JsonNode body,
        CancellationToken ct) =>
        await rc.WithExceptionCheck(async () =>
        await rc.WithStructuredContent(async () =>
    {
        var client = sp.GetRequiredService<IHttpClientFactory>().CreateClient();
        var url = BaseUrl + endpoint;
        var json = body.ToJsonString();

        using var req = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        await NotifyCall(rc, "POST", url, json);

        using var resp = await client.SendAsync(req, ct);
        return await HandleResponse(resp);
    }));

    private static async Task<CallToolResult?> ExecuteGetAsync(
        IServiceProvider sp,
        RequestContext<CallToolRequestParams> rc,
        string endpoint,
        CancellationToken ct) =>
        await rc.WithExceptionCheck(async () =>
        await rc.WithStructuredContent(async () =>
        {
            var client = sp.GetRequiredService<IHttpClientFactory>().CreateClient();
            var url = BaseUrl + endpoint;

            await NotifyCall(rc, "GET", url);

            using var resp = await client.GetAsync(url, ct);
            return await HandleResponse(resp);
        }));

    // =======================================================
    // Response + trace helpers
    // =======================================================
    private static async Task NotifyCall(
        RequestContext<CallToolRequestParams> rc,
        string method,
        string url,
        string? json = null)
    {
        var domain = new Uri(url).Host;
        var msg = json is null
            ? $"**{method}** `{domain}{url.Replace(BaseUrl, "")}`"
            : $"<details><summary>{method} {domain}{url.Replace(BaseUrl, "")}</summary>\n\n```json\n{Pretty(json)}\n```\n</details>";
        await rc.Server.SendMessageNotificationAsync(msg);
    }

    private static string Pretty(string json)
    {
        try
        {
            var node = JsonNode.Parse(json);
            return node?.ToJsonString(new JsonSerializerOptions { WriteIndented = true }) ?? json;
        }
        catch
        {
            return json;
        }
    }

    private static async Task<JsonNode?> HandleResponse(HttpResponseMessage resp)

    {
        var text = await resp.Content.ReadAsStringAsync();

        try
        {
            var parsed = JsonNode.Parse(text);
            return parsed;
        }
        catch
        {
            return new JsonObject
            {
                ["status"] = (int)resp.StatusCode,
                ["raw"] = text
            };
        }
    }
}
