using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using MCPhappey.Common.Extensions;
using MCPhappey.Core.Services;
using MCPhappey.Simplicate.Extensions;
using MCPhappey.Simplicate.Options;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace MCPhappey.Simplicate.Projects;

public static class SimplicateProjects
{
    [Description("Create a new project in Simplicate")]
    [McpServerTool(OpenWorld = false, Title = "Create new project in Simplicate")]
    public static async Task<CallToolResult?> SimplicateProjects_CreateProject(
        [Description("Name of the new project")] string name,
        IServiceProvider serviceProvider,
        RequestContext<CallToolRequestParams> requestContext,
        CancellationToken cancellationToken = default) => await serviceProvider.PostSimplicateResourceAsync(
        requestContext,
        "/projects/project",
        new SimplicateNewProject
        {
            Name = name
        },
        cancellationToken
    );

    [Description("Create a new project service in Simplicate")]
    [McpServerTool(OpenWorld = false, Title = "Create new project service in Simplicate")]
    public static async Task<CallToolResult?> SimplicateProjects_CreateProjectService(
    [Description("Name of the new project service")] string name,
    [Description("Id of the project")] string projectId,
    IServiceProvider serviceProvider,
    RequestContext<CallToolRequestParams> requestContext,
    CancellationToken cancellationToken = default) => await serviceProvider.PostSimplicateResourceAsync(
            requestContext,
            "/projects/projectservice",
            new SimplicateNewProjectService
            {
                Name = name,
                ProjectId = projectId
            },
            cancellationToken
    );

    [Description("Add a project employee in Simplicate")]
    [McpServerTool(OpenWorld = false, Title = "Add a project employee in Simplicate")]
    public static async Task<CallToolResult?> SimplicateProjects_AddProjectEmployee(
      [Description("Id of the project")] string projectId,
      [Description("Id of the employee")] string employeeId,
      IServiceProvider serviceProvider,
      RequestContext<CallToolRequestParams> requestContext,
      CancellationToken cancellationToken = default) => await serviceProvider.PostSimplicateResourceAsync(
        requestContext,
        "/projects/projectemployee",
        new SimplicateAddProjectEmployee
        {
            ProjectId = projectId,
            EmployeeId = employeeId
        },
        cancellationToken
    );

    [Description("Get projects grouped by project manager filtered by my organization profile, optionally filtered by date (equal or greater than), project.")]
    [McpServerTool(OpenWorld = false,
        Title = "Get projects by project manager",
        ReadOnly = true, UseStructuredContent = true)]
    public static async Task<Dictionary<string, IEnumerable<string>>?> SimplicateProjects_GetProjectNamesByProjectManager(
        IServiceProvider serviceProvider,
        RequestContext<CallToolRequestParams> requestContext,
        [Description("My organization profile name of the project filter. Optional.")] string myOrganizationProfileName,
        [Description("End date for filtering (on or after), format yyyy-MM-dd. Optional.")] string? date = null,
        [Description("Project status label to filter by. Optional.")] ProjectStatusLabel? projectStatusLabel = null,
        CancellationToken cancellationToken = default) => await GetProjectsGroupedBy(
            serviceProvider,
            requestContext,
            x => x.ProjectManager?.Name,
            myOrganizationProfileName, date, projectStatusLabel, cancellationToken);

    private static async Task<Dictionary<string, IEnumerable<string>>> GetProjectsGroupedBy(
            IServiceProvider serviceProvider,
            RequestContext<CallToolRequestParams> requestContext,
            Func<SimplicateProject, string?> groupKeySelector,
            string? myOrganizationProfileName = null,
            string? date = null,
            ProjectStatusLabel? projectStatusLabel = null,
            CancellationToken cancellationToken = default)
    {
        if (
             string.IsNullOrWhiteSpace(myOrganizationProfileName)
            && string.IsNullOrWhiteSpace(date))
            throw new ArgumentException("At least one filter (managerName, date, myOrganizationProfileName) must be provided.");

        var simplicateOptions = serviceProvider.GetRequiredService<SimplicateOptions>();
        var downloadService = serviceProvider.GetRequiredService<DownloadService>();

        string baseUrl = simplicateOptions.GetApiUrl("/projects/project");
        string select = "project_manager.,name";
        var filters = new List<string>();

        if (!string.IsNullOrWhiteSpace(date))
            filters.Add($"q[end_date][ge]={Uri.EscapeDataString(date)}");

        if (!string.IsNullOrWhiteSpace(myOrganizationProfileName)) filters.Add($"q[my_organization_profile.organization.name]=*{Uri.EscapeDataString(myOrganizationProfileName)}*");
        if (projectStatusLabel.HasValue) filters.Add($"q[project_status.label]=*{Uri.EscapeDataString(projectStatusLabel.Value.ToString())}*");

        var filterString = string.Join("&", filters) + $"&select={select}";

        var hours = await downloadService.GetAllSimplicatePagesAsync<SimplicateProject>(
            serviceProvider,
            requestContext.Server,
            baseUrl,
            filterString,
            pageNum => $"Downloading projects",
            requestContext,
            cancellationToken: cancellationToken
        );

        return hours
            .GroupBy(x => groupKeySelector(x) ?? string.Empty)
            .ToDictionary(
                g => g.Key,
                g => g.Select(t => t.Name)) ?? [];
    }

    [Description("Please fill in the project employee details")]
    public class SimplicateAddProjectEmployee
    {
        [JsonPropertyName("project_id")]
        [Required]
        [Description("The id of the project.")]
        public string? ProjectId { get; set; }

        [JsonPropertyName("employee_id")]
        [Required]
        [Description("The id of the employee.")]
        public string? EmployeeId { get; set; }
    }

    [Description("Please fill in the project service details")]
    public class SimplicateNewProjectService
    {
        [JsonPropertyName("name")]
        [Required]
        [Description("The name of the project service.")]
        public string? Name { get; set; }

        [JsonPropertyName("project_id")]
        [Required]
        [Description("The id of the project.")]
        public string? ProjectId { get; set; }

        [JsonPropertyName("track_hours")]
        [Required]
        [DefaultValue(true)]
        [Description("Track project service hours.")]
        public bool? TrackHours { get; set; } = true;

        [JsonPropertyName("track_cost")]
        [Required]
        [DefaultValue(true)]
        [Description("Track project service costs.")]
        public bool? TrackCost { get; set; } = true;

        [JsonPropertyName("vat_class_id")]
        [Required]
        [Description("Id of the vat class.")]
        public string VatClassId { get; set; } = default!;

        [JsonPropertyName("start_date")]
        [Description("Start date")]
        public DateTime? StartDate { get; set; }

        [JsonPropertyName("end_date")]
        [Description("End date.")]
        public DateTime? EndDate { get; set; }
    }

    [Description("Please fill in the project details")]
    public class SimplicateNewProject
    {
        [JsonPropertyName("name")]
        [Required]
        [Description("The name of the project.")]
        public string? Name { get; set; }
    }


    public enum ProjectStatusLabel
    {
        active,
        closed
    }

    public class SimplicateProject
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("project_manager")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public SimplicateProjectManager? ProjectManager { get; set; }

    }

    public class SimplicateProjectManager
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
    }

}

