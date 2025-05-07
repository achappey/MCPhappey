using System.ComponentModel;
using MCPhappey.Core.Extensions;
using ModelContextProtocol.Protocol.Types;
using RESTCountries.NET.Models;
using RESTCountries.NET.Services;

namespace MCPhappey.Tools.GitHub.RestCountries;

public static class CountryService
{
    [Description("Search country codes and names")]
    public static async Task<CallToolResponse> GitHubRestCountries_SearchCountryCodes(
        [Description("Search query by name (contains)")] string name)
    {
        var items = string.IsNullOrEmpty(name?.ToString())
                ? RestCountriesService.GetAllCountries()
                : RestCountriesService.GetCountriesByNameContains(name?.ToString() ?? string.Empty);

        var countries = items.Select(t => new { t.Name.Common, t.Name.Official, t.Cca2 });

        var result = System.Text.Json.JsonSerializer.Serialize(countries);

        return await Task.FromResult(result.ToTextCallToolResponse());
    }

    [Description("Get all country details by the alpha-2 code")]
    public static async Task<CallToolResponse> GitHubRestCountries_GetCountryDetail(
        [Description("The alpha-2 code of the country")] string cca2)
    {
        Country? result = RestCountriesService.GetCountryByCode(cca2.ToString()!);
        var resultJson = System.Text.Json.JsonSerializer.Serialize(result);

        return await Task.FromResult(resultJson.ToTextCallToolResponse());
    }
}

