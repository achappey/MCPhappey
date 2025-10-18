using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using MCPhappey.Core.Extensions;
using MCPhappey.Core.Services;
using MCPhappey.Simplicate.Extensions;
using MCPhappey.Simplicate.Options;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace MCPhappey.Simplicate.CRM;

public static class SimplicateCRM
{
    [McpServerTool(OpenWorld = false,
        ReadOnly = true,
        Destructive = false,
        Name = "simplicate_crm_get_organizations",
        Title = "Get organizations")]
    [Description("Get organizations, filtered by organization filters.")]
    public static async Task<CallToolResult?> SimplicateCRM_GetOrganizations(
     IServiceProvider serviceProvider,
     RequestContext<CallToolRequestParams> requestContext,
      [Description("Text value of industry name.")] string? industryName = null,
      [Description("Text value of relation type.")] string? relationType = null,
      [Description("(partial) text value of team name.")] string? teamName = null,
      [Description("Visiting address locality")] string? visitingAddressLocality = null,
      [Description("(partial) text value of the relation manager name")] string? relationManager = null,
      [Description("Offset used for pagination")] int? offset = null,
     CancellationToken cancellationToken = default)
     => await requestContext.WithExceptionCheck(async () =>
     await requestContext.WithStructuredContent(async () =>
     {
         var simplicateOptions = serviceProvider.GetRequiredService<SimplicateOptions>();
         var downloadService = serviceProvider.GetRequiredService<DownloadService>();

         string select = "industry.,name,email,note,id,url,phone,relation_type.,relation_manager.";
         var filters = new List<string>();

         if (!string.IsNullOrWhiteSpace(industryName))
             filters.Add($"q[industry.name]=*{Uri.EscapeDataString(industryName)}*");

         if (!string.IsNullOrWhiteSpace(relationType))
             filters.Add($"q[relation_type.label]=*{Uri.EscapeDataString(relationType)}*");

         if (!string.IsNullOrWhiteSpace(teamName))
             filters.Add($"q[teams.name]=*{Uri.EscapeDataString(teamName)}*");

         if (!string.IsNullOrWhiteSpace(relationManager))
             filters.Add($"q[relation_manager.name]=*{Uri.EscapeDataString(relationManager)}*");

         if (!string.IsNullOrWhiteSpace(visitingAddressLocality))
             filters.Add($"q[visiting_address.locality]=*{Uri.EscapeDataString(visitingAddressLocality)}*");

         if (offset.HasValue)
             filters.Add($"offset={offset.Value}");

         var filterString = string.Join("&", filters) + $"&select={select}&metadata=count,limit,offset&limit=100&sort=name";

         return await downloadService.GetSimplicatePageAsync<SimplicateOrganization>(
             serviceProvider, requestContext.Server,
             simplicateOptions.GetApiUrl("/crm/organization") + "?" + filterString,
             cancellationToken: cancellationToken);
     }));

    [McpServerTool(OpenWorld = false,
       ReadOnly = true,
       Destructive = false,
       Name = "simplicate_crm_get_organization_totals_by_industry",
       Title = "Get organization totals by industry")]
    [Description("Get organization totals grouped by industry, optionally filtered by organization filters.")]
    public static async Task<CallToolResult?> SimplicateCRM_GetOrganizationTotalsByIndustry(
       IServiceProvider serviceProvider,
       RequestContext<CallToolRequestParams> requestContext,
        [Description("Text value of relation type.")] string? relationType = null,
        [Description("(partial) text value of team name.")] string? teamName = null,
        [Description("(partial) text value of the relation manager name")] string? relationManager = null,
       CancellationToken cancellationToken = default)
       => await requestContext.WithExceptionCheck(async () =>
       await requestContext.WithStructuredContent(async () =>
       {
           var simplicateOptions = serviceProvider.GetRequiredService<SimplicateOptions>();
           var downloadService = serviceProvider.GetRequiredService<DownloadService>();

           string select = "industry.,name";
           var filters = new List<string>();

           if (!string.IsNullOrWhiteSpace(relationType))
               filters.Add($"q[relation_type.label]=*{Uri.EscapeDataString(relationType)}*");

           if (!string.IsNullOrWhiteSpace(teamName))
               filters.Add($"q[teams.name]=*{Uri.EscapeDataString(teamName)}*");

           if (!string.IsNullOrWhiteSpace(relationManager))
               filters.Add($"q[relation_manager.name]=*{Uri.EscapeDataString(relationManager)}*");

           var filterString = string.Join("&", filters) + $"&select={select}";

           var items = await downloadService.GetAllSimplicatePagesAsync<SimplicateOrganization>(
               serviceProvider, requestContext.Server,
               simplicateOptions.GetApiUrl("/crm/organization"),
               filterString,
               page => $"Downloading organizations page {page}",
               requestContext, cancellationToken: cancellationToken);

           return new
           {
               industries = items.GroupBy(a => a.Industry?.Name ?? "(onbekend)")
                .Select(z => new
                {
                    name = z.Key,
                    count = z.Count()
                })
           };
       }));

    [McpServerTool(OpenWorld = false,
        ReadOnly = true,
        Destructive = false,
        Name = "simplicate_crm_get_organization_totals_by_relation_type",
        Title = "Get organization totals by industry")]
    [Description("Get organization totals grouped by relation type, optionally filtered by organization filters.")]
    public static async Task<CallToolResult?> SimplicateCRM_GetOrganizationTotalsByRelationType(
        IServiceProvider serviceProvider,
        RequestContext<CallToolRequestParams> requestContext,
        [Description("(partial) text value of industry name.")] string? industryName = null,
        [Description("(partial) text value of team name.")] string? teamName = null,
        [Description("(partial) text value of the relation manager name")] string? relationManager = null,
        CancellationToken cancellationToken = default)
        => await requestContext.WithExceptionCheck(async () =>
     await requestContext.WithStructuredContent(async () =>
    {
        var simplicateOptions = serviceProvider.GetRequiredService<SimplicateOptions>();
        var downloadService = serviceProvider.GetRequiredService<DownloadService>();

        string select = "relation_type.,name";
        var filters = new List<string>();

        if (!string.IsNullOrWhiteSpace(industryName))
            filters.Add($"q[industry.name]=*{Uri.EscapeDataString(industryName)}*");

        if (!string.IsNullOrWhiteSpace(teamName))
            filters.Add($"q[teams.name]=*{Uri.EscapeDataString(teamName)}*");

        if (!string.IsNullOrWhiteSpace(relationManager))
            filters.Add($"q[relation_manager.name]=*{Uri.EscapeDataString(relationManager)}*");

        var filterString = string.Join("&", filters) + $"&select={select}";

        var items = await downloadService.GetAllSimplicatePagesAsync<SimplicateOrganization>(
            serviceProvider, requestContext.Server,
            simplicateOptions.GetApiUrl("/crm/organization"),
            filterString,
            page => $"Downloading organizations page {page}",
            requestContext, cancellationToken: cancellationToken);

        return new
        {
            relation_types = items.GroupBy(a => a.RelationType?.Label ?? "(onbekend)")
             .Select(z => new
             {
                 name = z.Key,
                 color = z.FirstOrDefault()?.RelationType?.Color,
                 count = z.Count()
             })
        };
    }));

    [Description("Get my organization profiles")]
    [McpServerTool(
     Title = "Get my organization profiles",
     Name = "simplicate_crm_get_my_organization_profiles",
     OpenWorld = false, ReadOnly = true)]
    public static async Task<CallToolResult?> SimplicateCRM_GetMyOrganzizationProfiles(
         IServiceProvider serviceProvider,
         RequestContext<CallToolRequestParams> requestContext,
         CancellationToken cancellationToken = default) => await requestContext.WithStructuredContent(async () =>
     {
         var simplicateOptions = serviceProvider.GetRequiredService<SimplicateOptions>();
         var downloadService = serviceProvider.GetRequiredService<DownloadService>();

         var items = await downloadService.GetAllSimplicatePagesAsync<SimplicateMyOrganizationProfile>(
             serviceProvider, requestContext.Server,
             simplicateOptions.GetApiUrl("/crm/myorganizationprofile"),
             string.Join("&", new[]
             {
                    "sort=name"
             }),
             page => $"Downloading my organization profiles page {page}",
             requestContext, cancellationToken: cancellationToken);

         return new SimplicateData<SimplicateMyOrganizationProfile>()
         {
             Data = items,
             Metadata = new SimplicateMetadata()
             {
                 Count = items.Count,
             }
         };
     });

    [Description("Create a new organization in Simplicate CRM")]
    [McpServerTool(Title = "Create new organization in Simplicate", Destructive = true, OpenWorld = false)]
    public static async Task<CallToolResult?> SimplicateCRM_CreateOrganization(
        [Description("The full name of the organization.")] string name,
        IServiceProvider serviceProvider,
        RequestContext<CallToolRequestParams> requestContext,
        [Description("A note or description about the organization.")] string? note = null,
        [Description("The primary email address for the organization.")] string? email = null,
        [Description("The main website URL of the organization.")] Uri? url = null,
        [Description("Industry id.")] string? industryId = null,
        CancellationToken cancellationToken = default) => await serviceProvider.PostSimplicateResourceAsync(
                requestContext,
                "/crm/organization",
               new SimplicateNewOrganization
               {
                   Name = name,
                   Note = note,
                   Email = email,
                   Url = url,
                   IndustryId = industryId
               },
                dto => new
                {
                    name = dto.Name,
                    note = dto.Note,
                    email = dto.Email,
                    url = dto.Url,
                    industry = !string.IsNullOrEmpty(dto.IndustryId) ? new
                    {
                        id = dto.IndustryId
                    } : null
                },
                cancellationToken
            );


    [Description("Create a new person in Simplicate CRM")]
    [McpServerTool(Title = "Create new person in Simplicate", Destructive = true, OpenWorld = false)]
    public static async Task<CallToolResult?> SimplicateCRM_CreatePerson(
        [Description("The person's first name.")] string firstName,
        [Description("The person's family name or surname.")] string familyName,
      IServiceProvider serviceProvider,
      RequestContext<CallToolRequestParams> requestContext,
        [Description("A note or comment about the person.")] string? note = null,
        [Description("The person's primary email address.")] string? email = null,
        [Description("The person's phone number.")] string? phone = null,
        [Description("The person's website URL, if available.")] Uri? websiteUrl = null,
      CancellationToken cancellationToken = default) => await serviceProvider.PostSimplicateResourceAsync(
        requestContext,
        "/crm/person",
        new SimplicateNewPerson
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

        [JsonPropertyName("industry.id")]
        [Description("Industry id")]
        public string? IndustryId { get; set; }
    }


    public class SimplicateMyOrganizationProfile
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = null!;

        [JsonPropertyName("organization_id")]
        public string OrganizationId { get; set; } = null!;

        [JsonPropertyName("name")]
        public string Name { get; set; } = null!;

        [JsonPropertyName("coc_code")]
        public string? CocCode { get; set; }

        [JsonPropertyName("vat_number")]
        public string? VatNumber { get; set; }

        [JsonPropertyName("bank_account")]
        public string? BankAccount { get; set; }

        [JsonPropertyName("blocked")]
        public bool? Blocked { get; set; }

        [JsonPropertyName("main_profile")]
        public bool? MainProfile { get; set; }
    }

    public class SimplicateOrganization
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = null!;

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("phone")]
        public string? Phone { get; set; }

        [JsonPropertyName("url")]
        public string? Url { get; set; }

        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("linkedin_url")]
        public string? LinkedinUrl { get; set; }

        [JsonPropertyName("coc_code")]
        public string? CocCode { get; set; }

        [JsonPropertyName("vat_number")]
        public string? VatNumber { get; set; }

        [JsonPropertyName("note")]
        public string? Note { get; set; }

        [JsonPropertyName("industry")]
        public SimplicateIndustry? Industry { get; set; }

        [JsonPropertyName("relation_type")]
        public SimplicateRelationType? RelationType { get; set; }

        [JsonPropertyName("relation_manager")]
        public SimplicateRelationManager? RelationManager { get; set; }
    }

    public class SimplicateRelationManager
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = null!;

        [JsonPropertyName("name")]
        public string? Name { get; set; } = null!;

    }

    public class SimplicateIndustry
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = null!;

        [JsonPropertyName("name")]
        public string? Name { get; set; } = null!;

    }

    public class SimplicateRelationType
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = null!;

        [JsonPropertyName("label")]
        public string? Label { get; set; } = null!;

        [JsonPropertyName("color")]
        public string Color { get; set; } = null!;

    }



}

