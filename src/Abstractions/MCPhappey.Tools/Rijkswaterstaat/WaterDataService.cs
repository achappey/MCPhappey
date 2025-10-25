using System.ComponentModel;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using MCPhappey.Common.Extensions;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using MCPhappey.Core.Extensions;

namespace MCPhappey.Tools.Rijkswaterstaat;

public static class WaterDataService
{
    private const string BaseUrl = "https://waterwebservices.beta.rijkswaterstaat.nl/test";

    // -------------------------------------------------------------------
    // ✅ TOOL 1: OphalenCatalogus (catalogus discovery)
    // -------------------------------------------------------------------
    [Description("Retrieve available water data catalog metadata (AQUO-based). Toggle which lists to include.")]
    [McpServerTool(
        Title = "Get WaterData catalog",
        Name = "rijkswaterstaat_waterdata_get_catalog",
        Idempotent = true,
        ReadOnly = true,
        OpenWorld = false,
        Destructive = false)]
    public static async Task<CallToolResult?> RijkswaterstaatWaterData_GetCatalog(
        [Description("Include Compartimenten list")] bool includeCompartimenten,
        [Description("Include Grootheden list")] bool includeGrootheden,
        [Description("Include Parameters list")] bool includeParameters = false,
        [Description("Include ProcesTypes list")] bool includeProcesTypes = false,
        [Description("Include Groeperingen list")] bool includeGroeperingen = false,
        [Description("Include Eenheden list")] bool includeEenheden = false,
        [Description("Include Hoedanigheden list")] bool includeHoedanigheden = false,
        [Description("Include Typeringen list")] bool includeTyperingen = false,
        [Description("Include WaardeBewerkingsMethoden list")] bool includeWaardeBewerkingsMethoden = false,
        [Description("Include BioTaxon list")] bool includeBioTaxon = false,
        [Description("Include Organen list")] bool includeOrganen = false,
        IServiceProvider serviceProvider = null!,
        RequestContext<CallToolRequestParams> requestContext = null!,
        CancellationToken cancellationToken = default)
        => await requestContext.WithExceptionCheck(async () =>
        {
            var filter = new JsonObject();
            AddBoolean(filter, "Compartimenten", includeCompartimenten);
            AddBoolean(filter, "Grootheden", includeGrootheden);
            AddBoolean(filter, "Parameters", includeParameters);
            AddBoolean(filter, "ProcesTypes", includeProcesTypes);
            AddBoolean(filter, "Groeperingen", includeGroeperingen);
            AddBoolean(filter, "Eenheden", includeEenheden);
            AddBoolean(filter, "Hoedanigheden", includeHoedanigheden);
            AddBoolean(filter, "Typeringen", includeTyperingen);
            AddBoolean(filter, "WaardeBewerkingsMethoden", includeWaardeBewerkingsMethoden);
            AddBoolean(filter, "BioTaxon", includeBioTaxon);
            AddBoolean(filter, "Organen", includeOrganen);

            var body = new JsonObject
            {
                ["CatalogusFilter"] = filter
            };

            return await ExecuteRwsPostAsync(
                requestContext,
                serviceProvider,
                "/METADATASERVICES/OphalenCatalogus",
                body,
                cancellationToken);
        });

    // -------------------------------------------------------------------
    // ✅ TOOL 2: OphalenWaarnemingen (historical & current data)
    // -------------------------------------------------------------------
    [Description("Retrieve water measurements by location, period and AQUO filters. Supports procesType, grouping (mux), quality codes, sampling heights and commissioning agencies.")]
    [McpServerTool(
        Title = "Get WaterData observations",
        Name = "rijkswaterstaat_waterdata_get_observations",
        Idempotent = true,
        ReadOnly = true,
        OpenWorld = false,
        Destructive = false)]
    public static async Task<CallToolResult?> WaterData_GetObservations(
        // Verplicht
        [Description("Location code, e.g. 'ameland.nes'")] string locatieCode,
        [Description("Begin datetime (ISO 8601), e.g. 2024-06-01T00:00:00.000+01:00")] DateTimeOffset beginTijd,
        [Description("End datetime (ISO 8601), e.g. 2025-01-01T00:00:00.000+01:00")] DateTimeOffset eindTijd,

        // AQUO metadata (optioneel)
        [Description("Compartiment code, e.g. 'OW'")] string? compartimentCode = null,
        [Description("Grootheid code, e.g. 'WATHTE' or 'CONCTTE'")] string? grootheidCode = null,
        [Description("Parameter code, e.g. 'Cd'")] string? parameterCode = null,
        [Description("ProcesType: 'meting' | 'verwachting' | 'astronomisch'")] string? procesType = null,
        [Description("Groepering code for multiplex (e.g. GETETBRKD2)")] string? groeperingCode = null,

        // Datakwaliteit en context (optioneel)
        [Description("List of quality codes to include, e.g. [\"00\",\"10\",\"20\",\"25\",\"30\",\"40\"]")]
        List<string>? kwaliteitswaardecodes = null,

        [Description("Sampling heights list, e.g. [\"-250\"]")] List<string>? bemonsteringshoogtes = null,

        [Description("Commissioning agencies list, e.g. [\"RIKZ_GOLVEN\"]")]
        List<string>? opdrachtgevendeInstanties = null,

        IServiceProvider serviceProvider = null!,
        RequestContext<CallToolRequestParams> requestContext = null!,
        CancellationToken cancellationToken = default)
        => await requestContext.WithExceptionCheck(async () =>
        {
            var aquoMetadata = new JsonObject();
            AddCode(aquoMetadata, "Compartiment", compartimentCode);
            AddCode(aquoMetadata, "Grootheid", grootheidCode);
            AddCode(aquoMetadata, "Parameter", parameterCode);
            AddString(aquoMetadata, "ProcesType", procesType);
            AddCode(aquoMetadata, "Groepering", groeperingCode);

            var waarnemingMetadata = new JsonObject();
            AddStringArray(waarnemingMetadata, "KwaliteitswaardecodeLijst", kwaliteitswaardecodes);
            AddStringArray(waarnemingMetadata, "BemonsteringshoogteLijst", bemonsteringshoogtes);
            AddStringArray(waarnemingMetadata, "OpdrachtgevendeInstantieLijst", opdrachtgevendeInstanties);

            var aquoPlus = new JsonObject
            {
                ["AquoMetadata"] = aquoMetadata
            };
            if (waarnemingMetadata.Count > 0)
                aquoPlus["WaarnemingMetadata"] = waarnemingMetadata;

            var body = new JsonObject
            {
                ["Locatie"] = new JsonObject { ["Code"] = locatieCode },
                ["AquoPlusWaarnemingMetadata"] = aquoPlus,
                ["Periode"] = new JsonObject
                {
                    ["Begindatumtijd"] = FormatIso(beginTijd),
                    ["Einddatumtijd"] = FormatIso(eindTijd)
                }
            };

            return await ExecuteRwsPostAsync(
                requestContext,
                serviceProvider,
                "/ONLINEWAARNEMINGENSERVICES/OphalenWaarnemingen",
                body,
                cancellationToken);
        });

    // -------------------------------------------------------------------
    // 🔹 OPTIONEEL: CheckWaarnemingenAanwezig (preflight check)
    // -------------------------------------------------------------------
    [Description("Quickly check if observations exist for locations and period before fetching full data.")]
    [McpServerTool(
        Title = "Check WaterData availability",
        Name = "rijkswaterstaat_waterdata_check_available",
        Idempotent = true,
        ReadOnly = true,
        OpenWorld = false,
        Destructive = false)]
    public static async Task<CallToolResult?> WaterData_CheckAvailable(
        [Description("List of location codes")] List<string> locatieCodes,
        [Description("Begin datetime (ISO 8601)")] DateTimeOffset beginTijd,
        [Description("End datetime (ISO 8601)")] DateTimeOffset eindTijd,
        // AQUO metadata (optioneel, sobere variant volgens docs)
        [Description("Compartiment code, e.g. 'OW'")] string? compartimentCode = null,
        [Description("Grootheid code, e.g. 'WATHTE'")] string? grootheidCode = null,

        IServiceProvider serviceProvider = null!,
        RequestContext<CallToolRequestParams> requestContext = null!,
        CancellationToken cancellationToken = default)
        => await requestContext.WithExceptionCheck(async () =>
        {
            var locatieLijst = new JsonArray(
                locatieCodes
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => (JsonNode)new JsonObject { ["Code"] = s })
            .ToArray()
    );

            var aquoMeta = new JsonObject();
            AddCode(aquoMeta, "Compartiment", compartimentCode);
            AddCode(aquoMeta, "Grootheid", grootheidCode);

            var aquoLijst = new JsonArray();
            if (aquoMeta.Count > 0)
                aquoLijst.Add(new JsonObject { ["AquoMetadata"] = aquoMeta });

            var body = new JsonObject
            {
                ["LocatieLijst"] = locatieLijst,
                ["Periode"] = new JsonObject
                {
                    ["Begindatumtijd"] = FormatIso(beginTijd),
                    ["Einddatumtijd"] = FormatIso(eindTijd)
                }
            };
            if (aquoLijst.Count > 0)
                body["AquoMetadataLijst"] = aquoLijst;

            return await ExecuteRwsPostAsync(
                requestContext,
                serviceProvider,
                "/ONLINEWAARNEMINGENSERVICES/CheckWaarnemingenAanwezig",
                body,
                cancellationToken);
        });

    // -------------------------------------------------------------------
    // 🔹 OPTIONEEL: OphalenLaatsteWaarnemingen (latest by combos)
    // -------------------------------------------------------------------
    [Description("Get latest valid measurements per provided location and AQUO metadata combinations (procesType is always 'meting' per docs).")]
    [McpServerTool(
        Title = "Get latest WaterData observations",
        Name = "rijkswaterstaat_waterdata_get_latest",
        Idempotent = true,
        ReadOnly = true,
        OpenWorld = false,
        Destructive = false)]
    public static async Task<CallToolResult?> WaterData_GetLatest(
        [Description("List of location codes")] List<string> locatieCodes,
        [Description("List of AQUO selector tuples (compartiment/grootheid/parameter optional). Each entry is one combination.")]
        List<LatestAquoSelector> aquoSelectors,
        IServiceProvider serviceProvider = null!,
        RequestContext<CallToolRequestParams> requestContext = null!,
        CancellationToken cancellationToken = default)
        => await requestContext.WithExceptionCheck(async () =>
        {
            var locatieLijst = new JsonArray(
      locatieCodes
          .Where(s => !string.IsNullOrWhiteSpace(s))
          .Select(s => (JsonNode)new JsonObject { ["Code"] = s })
          .ToArray()
  );

            var aquoPlusLijst = new JsonArray();
            foreach (var sel in aquoSelectors ?? new())
            {
                var meta = new JsonObject();
                AddCode(meta, "Compartiment", sel.CompartimentCode);
                AddCode(meta, "Grootheid", sel.GrootheidCode);
                AddCode(meta, "Parameter", sel.ParameterCode);
                // Per docs: endpoint retourneert enkel ProcesType=meting
                aquoPlusLijst.Add(new JsonObject
                {
                    ["AquoMetadata"] = meta
                });
            }

            var body = new JsonObject
            {
                ["LocatieLijst"] = locatieLijst,
                ["AquoPlusWaarnemingMetadataLijst"] = aquoPlusLijst
            };

            return await ExecuteRwsPostAsync(
                requestContext,
                serviceProvider,
                "/ONLINEWAARNEMINGENSERVICES/OphalenLaatsteWaarnemingen",
                body,
                cancellationToken);
        });

    // ================================================================
    // Shared HTTP executor (bewaart bruikbare 404-bodies, behandelt 204)
    // ================================================================
    private static async Task<CallToolResult?> ExecuteRwsPostAsync(
        RequestContext<CallToolRequestParams> requestContext,
        IServiceProvider serviceProvider,
        string endpoint,
        JsonNode body,
        CancellationToken cancellationToken)
        => await requestContext.WithStructuredContent(async () =>
    {
        var clientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
        using var client = clientFactory.CreateClient();

        var url = BaseUrl + endpoint;
        var json = body?.ToJsonString(new JsonSerializerOptions { WriteIndented = false }) ?? "{}";

        using var req = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        // Docs: optionele X-API-KEY (dummy) t.b.v. toekomstige differentiatie/debug
        req.Headers.Add("X-API-KEY", "dummy");
        req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        // nette trace in chat
        var domain = new Uri(url).Host;
        var markdown =
            $"<details><summary>POST <a href=\"{url}\" target=\"blank\">{domain}{endpoint}</a></summary>\n\n```json\n{JsonPrettify(json)}\n```\n</details>";
        await requestContext.Server.SendMessageNotificationAsync(markdown);

        using var resp = await client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        // 204: geen data gevonden
        if (resp.StatusCode == System.Net.HttpStatusCode.NoContent)
        {
            return new JsonObject
            {
                ["status"] = "no_content",
                ["message"] = "No data found for the provided query."
            };
        }

        // Lees payload altijd om 404-body bruikbaar te houden
        var payload = await resp.Content.ReadAsStringAsync(cancellationToken);

        // Success: probeer JSON te parsen
        if (resp.IsSuccessStatusCode)
        {
            try
            {
                return JsonNode.Parse(payload);
            }
            catch
            {
                return new JsonObject { ["status"] = "ok", ["raw"] = payload };
            }
        }

        // NotFound: docs zeggen dat er nog steeds nuttige body kan zijn
        if (resp.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            try
            {
                return JsonNode.Parse(payload);
            }
            catch
            {
                return new JsonObject
                {
                    ["status"] = "not_found",
                    ["raw"] = payload
                };
            }
        }

        // Overige errors: toon status + body zodat debugging intact blijft
        try
        {
            var parsed = JsonNode.Parse(payload);
            return new JsonObject
            {
                ["status"] = "error",
                ["httpStatus"] = (int)resp.StatusCode,
                ["response"] = parsed
            };
        }
        catch
        {
            return new JsonObject
            {
                ["status"] = "error",
                ["httpStatus"] = (int)resp.StatusCode,
                ["raw"] = payload
            };
        }
    });

    // ================================================================
    // Helpers (clean JSON opbouw)
    // ================================================================
    private static void AddBoolean(JsonObject obj, string name, bool value)
    {
        if (value) obj[name] = true;
    }

    private static void AddString(JsonObject obj, string name, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value)) obj[name] = value;
    }

    private static void AddCode(JsonObject obj, string propName, string? code)
    {
        if (!string.IsNullOrWhiteSpace(code))
            obj[propName] = new JsonObject { ["Code"] = code };
    }

    private static void AddStringArray(JsonObject obj, string name, List<string>? values)
    {
        if (values == null || values.Count == 0) return;
        var arr = new JsonArray(values.Where(v => !string.IsNullOrWhiteSpace(v)).Select(v => (JsonNode)v).ToArray());
        if (arr.Count > 0) obj[name] = arr;
    }

    private static string FormatIso(DateTimeOffset dto)
        => dto.ToString("yyyy-MM-dd'T'HH:mm:ss.fffzzz"); // conform voorbeelden met offset

    private static string JsonPrettify(string json)
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

    // ================================================================
    // DTO’s
    // ================================================================
    public class LatestAquoSelector
    {
        [Description("Compartiment code, e.g. 'OW'")]
        public string? CompartimentCode { get; set; }

        [Description("Grootheid code, e.g. 'T', 'WATHTE'")]
        public string? GrootheidCode { get; set; }

        [Description("Parameter code, e.g. 'Cd'")]
        public string? ParameterCode { get; set; }
    }
}
