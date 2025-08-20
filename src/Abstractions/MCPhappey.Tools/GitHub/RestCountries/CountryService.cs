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
    [McpServerTool(Title = "Search country codes and names",
        Name = "github_rest_countries_search_codes",
        Destructive = false,
        ReadOnly = true,
        Idempotent = true,
        OpenWorld = false)]
    public static async Task<EmbeddedResourceBlock> GitHubRestCountries_SearchCodes(
        [Description("Search query by name (contains)")] string name)
    {
        var items = string.IsNullOrEmpty(name?.ToString())
                ? RestCountriesService.GetAllCountries()
                : RestCountriesService.GetCountriesByNameContains(name?.ToString() ?? string.Empty);

        return await Task.FromResult(items
            .Select(t => new { t.Name.Common, t.Name.Official, t.Cca2 })
            .ToJsonContentBlock(SOURCE_URL));
    }

    [Description("Get all country details by the alpha-2 code")]
    [McpServerTool(Title = "Get all country details by the alpha-2 code",
        Name = "github_rest_countries_get_detail",
        Destructive = false,
        ReadOnly = true,
        Idempotent = true,
        OpenWorld = false,
        UseStructuredContent = true)]
    public static async Task<RESTCountries.NET.Models.Country?> GitHubRestCountries_GetDetail(
        [Description("The alpha-2 code of the country")] string cca2) =>
            await Task.FromResult(RestCountriesService
                .GetCountryByCode(cca2.ToString()!));

    [Description("Get countries by region")]
    [McpServerTool(Title = "Get countries by region",
        Name = "github_rest_countries_get_by_region",
        Destructive = false,
        Idempotent = true,
        ReadOnly = true,
        UseStructuredContent = true,
        OpenWorld = false)]
    public static async Task<IEnumerable<RESTCountries.NET.Models.Country>> GitHubRestCountries_GetByRegion(
        [Description("The region to filter on (e.g. Europe, Asia, Africa).")] string region) =>
            await Task.FromResult(RestCountriesService
                .GetAllCountries()
                .Where(a => a.Region.Equals(region, StringComparison.OrdinalIgnoreCase)));
}

