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
        Destructive = false,
        ReadOnly = true,
        OpenWorld = false)]
    public static async Task<EmbeddedResourceBlock> GitHubRestCountries_SearchCountryCodes(
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
        Destructive = false,
        ReadOnly = true,
        OpenWorld = false,
        UseStructuredContent = true)]
    public static async Task<RESTCountries.NET.Models.Country?> GitHubRestCountries_GetCountryDetail(
        [Description("The alpha-2 code of the country")] string cca2) =>
            await Task.FromResult(RestCountriesService
                .GetCountryByCode(cca2.ToString()!));

    [Description("Get countries by region")]
    [McpServerTool(Title = "Get countries by region",
        Destructive = false,
        ReadOnly = true,
        UseStructuredContent = true, OpenWorld = false)]
    public static async Task<IEnumerable<RESTCountries.NET.Models.Country>> GitHubRestCountries_GetCountriesByRegion(
        [Description("The region to filter on (e.g. Europe, Asia, Africa).")] string region) =>
            await Task.FromResult(RestCountriesService
                .GetAllCountries()
                .Where(a => a.Region.Equals(region, StringComparison.OrdinalIgnoreCase)));

}

