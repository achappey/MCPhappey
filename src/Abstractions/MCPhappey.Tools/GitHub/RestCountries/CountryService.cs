using System.ComponentModel;
using ModelContextProtocol.Server;
using RESTCountries.NET.Models;
using RESTCountries.NET.Services;

namespace MCPhappey.Tools.GitHub.RestCountries;

public static class CountryService
{
    [Description("Search country codes and names")]
    [McpServerTool(ReadOnly = true)]
    public static async Task<IEnumerable<object>> GitHubRestCountries_SearchCountryCodes(
        [Description("Search query by name (contains)")] string name)
    {
        var items = string.IsNullOrEmpty(name?.ToString())
                ? RestCountriesService.GetAllCountries()
                : RestCountriesService.GetCountriesByNameContains(name?.ToString() ?? string.Empty);

        return await Task.FromResult(items.Select(t => new { t.Name.Common, t.Name.Official, t.Cca2 }));
    }

    [Description("Get all country details by the alpha-2 code")]
    [McpServerTool(ReadOnly = true)]
    public static async Task<Country?> GitHubRestCountries_GetCountryDetail(
        [Description("The alpha-2 code of the country")] string cca2) =>
            await Task.FromResult(RestCountriesService.GetCountryByCode(cca2.ToString()!));
}

