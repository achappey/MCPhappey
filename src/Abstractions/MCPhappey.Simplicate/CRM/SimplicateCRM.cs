using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
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
    [McpServerTool(OpenWorld = false)]
    public static async Task<CallToolResult?> SimplicateCRM_CreateOrganization(
        [Description("The full name of the organization.")] string name,
        IServiceProvider serviceProvider,
        RequestContext<CallToolRequestParams> requestContext,
        [Description("A note or description about the organization.")] string? note = null,
        [Description("The primary email address for the organization.")] string? email = null,
        [Description("The main website URL of the organization.")] Uri? url = null,
        CancellationToken cancellationToken = default)
    {
        var simplicateOptions = serviceProvider.GetRequiredService<SimplicateOptions>();

        // Simplicate CRM Organization endpoint
        string baseUrl = simplicateOptions.GetApiUrl("/crm/organization");

        var (dtoItem, notAccepted) = await requestContext.Server.TryElicit<SimplicateNewOrganization>(
                new SimplicateNewOrganization
                {
                    Name = name,
                    Note = note,
                    Email = email,
                    Url = url
                },
                cancellationToken
            );

        if (notAccepted != null) return notAccepted;

        // Use your POST extension to create the org
        return (await serviceProvider.PostSimplicateItemAsync(
            baseUrl,
            dtoItem,
            requestContext: requestContext,
            cancellationToken: cancellationToken
        ))?.ToCallToolResult();
    }

    [Description("Create a new person in Simplicate CRM")]
    [McpServerTool(OpenWorld = false)]
    public static async Task<CallToolResult?> SimplicateCRM_CreatePerson(
        [Description("The person's first name.")] string firstName,
        [Description("The person's family name or surname.")] string familyName,
      IServiceProvider serviceProvider,
      RequestContext<CallToolRequestParams> requestContext,
        [Description("A note or comment about the person.")] string? note = null,
        [Description("The person's primary email address.")] string? email = null,
        [Description("The person's phone number.")] string? phone = null,
        [Description("The person's website URL, if available.")] Uri? websiteUrl = null,
      CancellationToken cancellationToken = default)
    {
        var simplicateOptions = serviceProvider.GetRequiredService<SimplicateOptions>();

        string baseUrl = simplicateOptions.GetApiUrl("/crm/person");
        var (dtoItem, notAccepted) = await requestContext.Server.TryElicit<SimplicateNewPerson>(
               new SimplicateNewPerson()
               {
                   FirstName = firstName,
                   FamilyName = familyName,
                   Note = note,
                   Email = email,
                   Phone = phone,
                   WebsiteUrl = websiteUrl
               },
               cancellationToken
           );

        if (notAccepted != null) return notAccepted;

        return (await serviceProvider.PostSimplicateItemAsync(
            baseUrl,
            dtoItem,
            requestContext: requestContext,
            cancellationToken: cancellationToken
        ))?.ToCallToolResult();

    }

    [Description("Please fill in the person details")]
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


    [Description("Please fill in the organization details")]
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

