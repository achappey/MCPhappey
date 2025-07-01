using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;
using MCPhappey.Common;
using MCPhappey.Common.Extensions;
using MCPhappey.Simplicate.Extensions;
using MCPhappey.Simplicate.Options;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace MCPhappey.Simplicate.CRM;

public static class SimplicateCRM
{
    [Description("Create a new organization in Simplicate CRM")]
    [McpServerTool(ReadOnly = false, Idempotent = false, UseStructuredContent = true)]
    public static async Task<SimplicateNewItemData?> SimplicateCRM_CreateOrganization(
        IServiceProvider serviceProvider,
        RequestContext<CallToolRequestParams> requestContext,
        CancellationToken cancellationToken = default)
    {
        var simplicateOptions = serviceProvider.GetRequiredService<SimplicateOptions>();
        var scrapers = serviceProvider.GetServices<IContentScraper>();
        var scraper = scrapers.OfType<SimplicateScraper>().First();

        // Simplicate CRM Organization endpoint
        string baseUrl = $"https://{simplicateOptions.Organization}.simplicate.app/api/v2/crm/organization";
        var elicitParams = "Please fill in the organization details".CreateElicitRequestParamsForType<SimplicateNewOrganization>();

        // Nu kun je hem zo gebruiken:
        var elicitResult = await requestContext.Server.ElicitAsync(elicitParams, cancellationToken: cancellationToken);
        elicitResult.EnsureAccept();

        var dto = JsonSerializer.Deserialize<SimplicateNewOrganization>(
            JsonSerializer.Serialize(elicitResult.Content)
        );

        // Use your POST extension to create the org
        return await scraper.PostSimplicateItemAsync(
            serviceProvider,
            baseUrl,
            dto!,
            requestContext: requestContext,
            cancellationToken: cancellationToken
        );
    }

    [Description("Create a new person in Simplicate CRM")]
    [McpServerTool(ReadOnly = false, Idempotent = false, UseStructuredContent = true)]
    public static async Task<SimplicateNewItemData?> SimplicateCRM_CreatePerson(
      IServiceProvider serviceProvider,
      RequestContext<CallToolRequestParams> requestContext,
      CancellationToken cancellationToken = default)
    {
        var simplicateOptions = serviceProvider.GetRequiredService<SimplicateOptions>();
        var scrapers = serviceProvider.GetServices<IContentScraper>();
        var scraper = scrapers.OfType<SimplicateScraper>().First();

        // Simplicate CRM Organization endpoint
        string baseUrl = $"https://{simplicateOptions.Organization}.simplicate.app/api/v2/crm/person";
        var elicitParams = "Please fill in the organization details".CreateElicitRequestParamsForType<SimplicateNewPerson>();

        var elicitResult = await requestContext.Server.ElicitAsync(elicitParams, cancellationToken: cancellationToken);

        elicitResult.EnsureAccept();

        var dto = JsonSerializer.Deserialize<SimplicateNewPerson>(
            JsonSerializer.Serialize(elicitResult.Content)
        );
        
        return await scraper.PostSimplicateItemAsync(
            serviceProvider,
            baseUrl,
            dto!,
            requestContext: requestContext,
            cancellationToken: cancellationToken
        );

    }

    public class SimplicateNewPerson
    {
        [JsonPropertyName("first_name")]
        [Required]
        [Description("The person's first name.")]
        public string? FirstName { get; set; }

        [JsonPropertyName("family_name")]
        [Required]
        [Description("The person's family name or surname.")]
        public string? FamilyName { get; set; }

        [JsonPropertyName("note")]
        [Description("A note or comment about the person.")]
        public string? Note { get; set; }

        [JsonPropertyName("email")]
        [EmailAddress]
        [Description("The person's primary email address.")]
        public string? Email { get; set; }

        [JsonPropertyName("phone")]
        [Description("The person's phone number.")]
        public string? Phone { get; set; }

        [JsonPropertyName("website_url")]
        [Description("The person's website URL, if available.")]
        public Uri? WebsiteUrl { get; set; }
    }


    public class SimplicateNewOrganization
    {
        [JsonPropertyName("name")]
        [Required]
        [Description("The full name of the organization.")]
        public string? Name { get; set; }

        [JsonPropertyName("note")]
        [Description("A note or description about the organization.")]
        public string? Note { get; set; }

        [JsonPropertyName("email")]
        [EmailAddress]
        [Description("The primary email address for the organization.")]
        public string? Email { get; set; }

        [JsonPropertyName("url")]
        [Description("The main website URL of the organization.")]
        public Uri? Url { get; set; }
    }

}

