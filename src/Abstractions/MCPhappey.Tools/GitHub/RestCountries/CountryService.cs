using System.ComponentModel;
using MCPhappey.Common.Extensions;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using RESTCountries.NET.Services;

namespace MCPhappey.Tools.GitHub.RestCountries;

public static class CountryService
{
    static readonly string SOURCE_URL = "https://github.com/egbakou/RESTCountries.NET";

    [Description("Search country codes and names")]
    [McpServerTool(Name = "GitHubRestCountries_SearchCountryCodes", ReadOnly = true)]
    public static async Task<EmbeddedResourceBlock> GitHubRestCountries_SearchCountryCodes(
        [Description("Search query by name (contains)")] string name,
        RequestContext<CallToolRequestParams> requestContext)
    {
        var items = string.IsNullOrEmpty(name?.ToString())
                ? RestCountriesService.GetAllCountries()
                : RestCountriesService.GetCountriesByNameContains(name?.ToString() ?? string.Empty);

        return await Task.FromResult(items.Select(t => new { t.Name.Common, t.Name.Official, t.Cca2 }).ToJsonContentBlock(SOURCE_URL));
    }

    [Description("Get all country details by the alpha-2 code")]
    [McpServerTool(Name = "GitHubRestCountries_GetCountryDetail", ReadOnly = true)]
    public static async Task<EmbeddedResourceBlock?> GitHubRestCountries_GetCountryDetail(
        [Description("The alpha-2 code of the country")] string cca2) =>
            await Task.FromResult(RestCountriesService.GetCountryByCode(cca2.ToString()!).ToJsonContentBlock(SOURCE_URL));
}

