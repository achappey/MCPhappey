using System.ComponentModel;
using MCPhappey.Common.Extensions;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using RESTCountries.NET.Services;

namespace MCPhappey.Tools.GitHub.RestCountries;

public static class CountryService
{
    static string SOURCE_URL = "https://github.com/egbakou/RESTCountries.NET";

    [Description("Search country codes and names")]
    [McpServerTool(ReadOnly = true)]
    public static async Task<EmbeddedResourceBlock> GitHubRestCountries_SearchCountryCodes(
        [Description("Search query by name (contains)")] string name,
        RequestContext<CallToolRequestParams> requestContext)
    {
        var result = await requestContext.Server.ElicitAsync(new ElicitRequestParams()
        {
            Message = "please fill in this form",
            RequestedSchema = new ElicitRequestParams.RequestSchema()
            {
                Properties = new Dictionary<string, ElicitRequestParams.PrimitiveSchemaDefinition>()
                {
                    { "asdas", new ElicitRequestParams.StringSchema()}
                }
            }
        });
        var req1 = requestContext.Server.ElicitAsync(new ElicitRequestParams
        {
            Message = "Please enter your first name",
            RequestedSchema = new ElicitRequestParams.RequestSchema
            {
                Properties = new Dictionary<string, ElicitRequestParams.PrimitiveSchemaDefinition>
        {
            { "firstName", new ElicitRequestParams.StringSchema { Title = "First Name" } }
        }
            }
        }).AsTask();
        
        var req2 = requestContext.Server.ElicitAsync(new ElicitRequestParams
        {
            Message = "Please enter your last name",
            RequestedSchema = new ElicitRequestParams.RequestSchema
            {
                Properties = new Dictionary<string, ElicitRequestParams.PrimitiveSchemaDefinition>
        {
            { "lastName", new ElicitRequestParams.StringSchema { Title = "Last Name" } }
        }
            }
        }).AsTask();

        var results = await Task.WhenAll(req1, req2);
        var items = string.IsNullOrEmpty(name?.ToString())
                ? RestCountriesService.GetAllCountries()
                : RestCountriesService.GetCountriesByNameContains(name?.ToString() ?? string.Empty);

        return await Task.FromResult(items.Select(t => new { t.Name.Common, t.Name.Official, t.Cca2 }).ToJsonContentBlock(SOURCE_URL));
    }

    [Description("Get all country details by the alpha-2 code")]
    [McpServerTool(ReadOnly = true)]
    public static async Task<EmbeddedResourceBlock?> GitHubRestCountries_GetCountryDetail(
        [Description("The alpha-2 code of the country")] string cca2) =>
            await Task.FromResult(RestCountriesService.GetCountryByCode(cca2.ToString()!).ToJsonContentBlock(SOURCE_URL));
}

