using System.ComponentModel;
using System.Text.Json.Serialization;
using MCPhappey.Core.Services;
using MCPhappey.Simplicate.Extensions;
using MCPhappey.Simplicate.Options;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace MCPhappey.Simplicate.HRM;

public static class SimplicateHRM
{
    [Description("Get Simplicate leaves totals grouped on employee and leave type")]
    [McpServerTool(Title = "Get Simplicate leave totals", OpenWorld = false, ReadOnly = true, UseStructuredContent = true)]
    public static async Task<Dictionary<string, List<LeaveTotals>>?> SimplicateHRM_GetLeaveTotals(
        [Description("Team to get the leave totals for")] string teamName,
        IServiceProvider serviceProvider,
        RequestContext<CallToolRequestParams> requestContext,
        [Description("Filter on leave type")] string? leaveType = null,
        CancellationToken cancellationToken = default)
    {
        var simplicateOptions = serviceProvider.GetRequiredService<SimplicateOptions>();
        var downloadService = serviceProvider.GetRequiredService<DownloadService>();

        string employeeUrl = simplicateOptions.GetApiUrl("/hrm/employee");

        string employeeSelect = "id";
        var employeeFilters = new List<string>
        {
            "q[employment_status]=active",
            $"q[teams.name]=*{teamName}",
            "q[is_user]=true",
            "q[type.label]=internal",
            $"select={employeeSelect}"
        };

        var employeeFilterString = string.Join("&", employeeFilters);
        var employees = await downloadService.GetAllSimplicatePagesAsync<SimplicateIdItem>(
                  serviceProvider,
                  requestContext.Server,
                  employeeUrl,
                  employeeFilterString,
                  pageNum => $"Downloading employees {teamName} leaves page {pageNum}",
                  requestContext,
                  cancellationToken: cancellationToken
              );

        var selectedId = employees.OfType<SimplicateIdItem>().Select(a => a.Id);
        string timetableUrl = simplicateOptions.GetApiUrl("/hrm/timetable");
        string timetableSelect = "even_week,odd_week,employee.,end_date";
        var timetableFilters = new List<string>
        {
            $"q[end_date]=null",
            $"select={timetableSelect}"
        };

        var timetableFilterString = string.Join("&", timetableFilters);
        var timetables = await downloadService.GetAllSimplicatePagesAsync<SimplicateTimetable>(
                  serviceProvider,
                  requestContext.Server,
                  timetableUrl,
                  timetableFilterString,
                  pageNum => $"Downloading timetables page {pageNum}",
                  requestContext,
                  cancellationToken: cancellationToken
              );

        string baseUrl = simplicateOptions.GetApiUrl("/hrm/leave");
        string select = "employee.,hours,leavetype.,start_date";
        var filters = new List<string>
        {
            $"select={select}"
        };

        var filterString = string.Join("&", filters);

        // Typed DTO ophalen via extension method
        var leaves = await downloadService.GetAllSimplicatePagesAsync<SimplicateLeave>(
            serviceProvider,
            requestContext.Server,
            baseUrl,
            filterString,
            pageNum => $"Downloading HRM leaves page {pageNum}",
            requestContext, // Geen requestContext want geen progress nodig, of voeg eventueel toe!
            cancellationToken: cancellationToken
        );

        // Extra filter op leaveType (optioneel)
        if (!string.IsNullOrEmpty(leaveType))
        {
            leaves = [.. leaves.Where(a => a.LeaveType?.Label?.Contains(leaveType, StringComparison.OrdinalIgnoreCase) == true)];
        }

        var timetableLookup = timetables
              .GroupBy(t => t.Employee.Id)
              .ToDictionary(g => g.Key, g => g.Average(z => z.AverageWorkdayHours));

        return leaves
            .Where(a => selectedId.Contains(a.Employee?.Id))
            .GroupBy(x => new
            {
                EmployeeId = x.Employee?.Id,
                EmployeeName = x.Employee?.Name ?? "",
                AverageWorkdayHours = timetableLookup.TryGetValue(x.Employee?.Id ?? "", out var t)
                    ? t : 0
            })
            .OrderBy(x => x.Key.EmployeeName)
            .ToDictionary(
                g => g.Key.EmployeeName,
                g => g.GroupBy(x => x.LeaveType?.Label ?? "")
                    .Select(lg => new LeaveTotals
                    {
                        LeaveType = lg.Key,
                        TotalHours = lg.Sum(x => x.Hours),
                        TotalHoursPlanned = lg
                            .Where(a => a.StartDate != null
                                && DateTime.Parse(a.StartDate) > DateTime.Now)
                            .Sum(x => x.Hours),
                        TotalDays = g.Key.AverageWorkdayHours > 0 ?
                            lg.Sum(x => x.Hours) / g.Key.AverageWorkdayHours
                            : 0
                    })
                    .ToList()
            );

    }

    public class SimplicateTimetable
    {
        [JsonPropertyName("employee")]
        public Employee Employee { get; set; } = null!;

        [JsonPropertyName("even_week")]
        public WeekSchedule EvenWeek { get; set; } = null!;

        [JsonPropertyName("odd_week")]
        public WeekSchedule OddWeek { get; set; } = null!;

        [JsonIgnore]
        public double AverageWorkdayHours
        {
            get
            {
                var allDays = EvenWeek.AllDays.Concat(OddWeek.AllDays).ToList();
                var workedDays = allDays.Where(d => d.Hours > 0).ToList();
                if (workedDays.Count == 0) return 0;
                return workedDays.Sum(d => d.Hours) / workedDays.Count;
            }
        }
    }

    public class WeekSchedule
    {
        [JsonIgnore]
        public DaySchedule[] AllDays =>
            [Day1, Day2, Day3, Day4, Day5, Day6, Day7];

        [JsonPropertyName("day_1")]
        public DaySchedule Day1 { get; set; } = null!;

        [JsonPropertyName("day_2")]
        public DaySchedule Day2 { get; set; } = null!;

        [JsonPropertyName("day_3")]
        public DaySchedule Day3 { get; set; } = null!;

        [JsonPropertyName("day_4")]
        public DaySchedule Day4 { get; set; } = null!;

        [JsonPropertyName("day_5")]
        public DaySchedule Day5 { get; set; } = null!;

        [JsonPropertyName("day_6")]
        public DaySchedule Day6 { get; set; } = null!;

        [JsonPropertyName("day_7")]
        public DaySchedule Day7 { get; set; } = null!;
    }

    public class DaySchedule
    {
        [JsonPropertyName("start_time")]
        public double StartTime { get; set; }

        [JsonPropertyName("end_time")]
        public double EndTime { get; set; }

        [JsonPropertyName("hours")]
        public double Hours { get; set; }
    }

    public class SimplicateLeave
    {
        [JsonPropertyName("employee")]
        public Employee? Employee { get; set; }

        [JsonPropertyName("leavetype")]
        public LeaveType? LeaveType { get; set; }

        [JsonPropertyName("start_date")]
        public string? StartDate { get; set; }

        [JsonPropertyName("hours")]
        public double Hours { get; set; }
    }

    public class SimplicateIdItem
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;
    }

    public class Employee
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;
    }

    public class LeaveType
    {
        [JsonPropertyName("label")]
        public string Label { get; set; } = string.Empty;
    }

    public class LeaveTotals
    {
        [JsonPropertyName("leavetype")]
        public string LeaveType { get; set; } = string.Empty;

        [JsonPropertyName("totalDays")]
        public double TotalDays { get; set; }

        [JsonPropertyName("totalHours")]
        public double TotalHours { get; set; }

        [JsonPropertyName("totalHoursPlanned")]
        public double TotalHoursPlanned { get; set; }


    }
}

