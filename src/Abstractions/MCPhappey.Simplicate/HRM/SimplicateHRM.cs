using System.ComponentModel;
using System.Text.Json.Serialization;
using MCPhappey.Core.Extensions;
using MCPhappey.Core.Services;
using MCPhappey.Simplicate.Extensions;
using MCPhappey.Simplicate.Options;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace MCPhappey.Simplicate.HRM;

public static class SimplicateHRM
{
    [Description("Get Simplicate employees")]
    [McpServerTool(
       Title = "Get Simplicate employees",
       Name = "simplicate_hrm_get_team_employees",
       OpenWorld = false, ReadOnly = true)]
    public static async Task<CallToolResult?> SimplicateHRM_GetTeamEmployees(
           [Description("Team to get the leave totals for")] string teamName,
           IServiceProvider serviceProvider,
           RequestContext<CallToolRequestParams> requestContext,
           [Description("Filter on employment status")] string? employmentStatus = "active",
           [Description("Filter on is user")] bool isUser = true,
           CancellationToken cancellationToken = default) => await requestContext.WithStructuredContent(async () =>
       {
           var simplicateOptions = serviceProvider.GetRequiredService<SimplicateOptions>();
           var downloadService = serviceProvider.GetRequiredService<DownloadService>();

           // 1) Employees (IDs only)
           var employees = await downloadService.GetAllSimplicatePagesAsync<SimplicateEmployee>(
               serviceProvider, requestContext.Server,
               simplicateOptions.GetApiUrl("/hrm/employee"),
               string.Join("&", new[]
               {
                    $"q[employment_status]={employmentStatus}",
                    $"q[teams.name]=*{teamName}*",
                    $"q[is_user]={isUser.ToString()?.ToLower()}",
                    "sort=name"
               }),
               page => $"Downloading employees {teamName} page {page}",
               requestContext, cancellationToken: cancellationToken);

           return new SimplicateData<SimplicateEmployee>()
           {
               Data = employees,
               Metadata = new SimplicateMetadata()
               {
                   Count = employees.Count,
               }
           };
       });

    [Description("Get Simplicate leaves totals grouped on employee and leave type")]
    [McpServerTool(
    Title = "Get Simplicate leave totals",
    Name = "simplicate_hrm_get_leave_totals",
    OpenWorld = false, ReadOnly = true, UseStructuredContent = true)]
    public static async Task<CallToolResult?> SimplicateHRM_GetLeaveTotals(
        [Description("Team to get the leave totals for")] string teamName,
        IServiceProvider serviceProvider,
        RequestContext<CallToolRequestParams> requestContext,
        [Description("Filter on leave type")] string? leaveType = null,
        CancellationToken cancellationToken = default) => await requestContext.WithStructuredContent(async () =>
    {
        var simplicateOptions = serviceProvider.GetRequiredService<SimplicateOptions>();
        var downloadService = serviceProvider.GetRequiredService<DownloadService>();

        // 1) Employees (IDs only)
        var employees = await downloadService.GetAllSimplicatePagesAsync<SimplicateIdItem>(
            serviceProvider, requestContext.Server,
            simplicateOptions.GetApiUrl("/hrm/employee"),
            string.Join("&", new[]
            {
            "q[employment_status]=active",
            $"q[teams.name]=*{teamName}",
            "q[is_user]=true",
            "q[type.label]=internal",
            "select=id"
            }),
            page => $"Downloading employees {teamName} page {page}",
            requestContext, cancellationToken: cancellationToken);

        var selectedIds = new HashSet<string>(employees.Select(e => e.Id ?? string.Empty)
                                                      .Where(id => !string.IsNullOrEmpty(id)));

        // 2) Timetables -> average DAILY hours per employee
        var timetables = await downloadService.GetAllSimplicatePagesAsync<SimplicateTimetable>(
            serviceProvider, requestContext.Server,
            simplicateOptions.GetApiUrl("/hrm/timetable"),
            string.Join("&", new[] { "q[end_date]=null", "select=even_week,odd_week,employee.,end_date" }),
            page => $"Downloading timetables page {page}",
            requestContext, cancellationToken: cancellationToken);

        // NOTE: We assume SimplicateTimetable.AverageWorkdayHours is "hours per workday".
        // If it is weekly, fix here by converting to daily.
        double CleanDaily(double v) => (v > 0 && v <= 16) ? v : double.NaN;

        var avgDailyHoursByEmp = timetables
            .Where(t => !string.IsNullOrEmpty(t.Employee?.Id))
            .GroupBy(t => t.Employee!.Id)
            .ToDictionary(
                g => g.Key,
                g =>
                {
                    var daily = g.Select(z => CleanDaily(z.AverageWorkdayHours)).Where(x => !double.IsNaN(x));
                    var avg = daily.Any() ? daily.Average() : double.NaN;
                    // final guardrail: if missing/insane, fallback to 8h/day
                    return double.IsNaN(avg) || avg <= 0 || avg > 16 ? 8.0 : Math.Round(avg, 2);
                });

        // 3) Leaves
        var leaves = await downloadService.GetAllSimplicatePagesAsync<SimplicateLeave>(
            serviceProvider, requestContext.Server,
            simplicateOptions.GetApiUrl("/hrm/leave"),
            "",// "select=employee.,hours,leavetype.,start_date,leave_status.",
            page => $"Downloading HRM leaves page {page}",
            requestContext, cancellationToken: cancellationToken);

        var dsds = leaves.GroupBy(a => a.LeaveType?.Label).Select(a => a.Key);
        var dsds2 = leaves.GroupBy(a => a.LeaveType?.Label);
        if (!string.IsNullOrWhiteSpace(leaveType))
        {
            leaves = [.. leaves.Where(a =>
            a.LeaveType?.Label?.Contains(leaveType, StringComparison.OrdinalIgnoreCase) == true)];
        }



        // 4) Shape output
        var result = leaves
            .Where(a => a.Employee?.Id != null && selectedIds.Contains(a.Employee.Id))
            .GroupBy(x => new
            {
                EmployeeId = x.Employee!.Id,
                EmployeeName = x.Employee!.Name ?? ""
            })
            //.OrderBy(g => g.Key.EmployeeName)
            .Select(empGroup =>
            {
                // stable, per-employee daily hours divisor
                var avgHours = avgDailyHoursByEmp.TryGetValue(empGroup.Key.EmployeeId, out var h) ? h : 8.0;

                // compute "planned" against UTC now (avoid local/offset mismatches)
                var nowUtc = DateTime.UtcNow;

                var leavesByType = empGroup
                    .GroupBy(x => x.LeaveType?.Label ?? "")
                    .Select(lg =>
                    {
                        var sumHours = lg.Sum(x => x.Hours);
                        var plannedHours = lg
                            .Where(a =>
                            {
                                if (string.IsNullOrEmpty(a.StartDate)) return false;
                                if (!DateTimeOffset.TryParse(a.StartDate, out var dto)) return false;
                                return dto.UtcDateTime > nowUtc;
                            })
                            .Sum(x => x.Hours);

                        return new LeaveTotals
                        {
                            // LeaveType = lg.Key,            // NOTE: uses DTO property name (lower field is auto-gen’d)
                            LeaveType = lg.Key,            // set both in case of older serializers
                            TotalHours = sumHours,
                            TotalHoursPlanned = plannedHours,
                            TotalDays = Math.Round(sumHours / avgHours, 2)
                        };
                    })
                    .ToList();

                return new
                {
                    employee_name = empGroup.Key.EmployeeName,
                    avgWorkdayHours = avgHours,      // 👈 include for quick sanity check in UI
                    leaves = leavesByType
                };
            })
            .ToList();

        return new { leaves = result };
    });

    /*
        [Description("Get Simplicate leaves totals grouped on employee and leave type")]
        [McpServerTool(Title = "Get Simplicate leave totals",
            Name = "simplicate_hrm_get_leave_totals",
            OpenWorld = false, ReadOnly = true, UseStructuredContent = true)]
        public static async Task<CallToolResult?> SimplicateHRM_GetLeaveTotals2(
            [Description("Team to get the leave totals for")] string teamName,
            IServiceProvider serviceProvider,
            RequestContext<CallToolRequestParams> requestContext,
            [Description("Filter on leave type")] string? leaveType = null,
            CancellationToken cancellationToken = default) => await requestContext.WithStructuredContent(async () =>
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

            return new
            {
                leaves = leaves
               .Where(a => selectedId.Contains(a.Employee?.Id))
               .GroupBy(x => new
               {
                   EmployeeId = x.Employee?.Id,
                   EmployeeName = x.Employee?.Name ?? "",
                   AverageWorkdayHours = timetableLookup.TryGetValue(x.Employee?.Id ?? "", out var t)
                       ? t : 0
               })
               .OrderBy(x => x.Key.EmployeeName)
               .Select(empGroup =>
               {
                   var avgHours = empGroup.Key.AverageWorkdayHours;

                   return new
                   {
                       employee_name = empGroup.Key.EmployeeName,
                       leaves = empGroup
                           .GroupBy(x => x.LeaveType?.Label ?? "")
                           .Select(lg => new LeaveTotals
                           {
                               LeaveType = lg.Key,
                               TotalHours = lg.Sum(x => x.Hours),
                               TotalHoursPlanned = lg
                                   .Where(a => a.StartDate != null && DateTime.Parse(a.StartDate) > DateTime.Now)
                                   .Sum(x => x.Hours),
                               TotalDays = avgHours > 0 ? lg.Sum(x => x.Hours) / avgHours : 0
                           })
                           .ToList()
                   };
               })
            };


        });*/

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

        [JsonPropertyName("leave_status")]
        public LeaveStatus? LeaveStatus { get; set; }

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

    public class LeaveStatus
    {
        [JsonPropertyName("label")]
        public string Label { get; set; } = string.Empty;
    }

    public class LeaveTotals
    {
        [JsonPropertyName("leaveType")]
        public string LeaveType { get; set; } = string.Empty;

        [JsonPropertyName("totalDays")]
        public double TotalDays { get; set; }

        [JsonPropertyName("totalHours")]
        public double TotalHours { get; set; }

        [JsonPropertyName("totalHoursPlanned")]
        public double TotalHoursPlanned { get; set; }
    }

    public class SimplicateEmployee
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("person_id")]
        public string PersonId { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("bank_account")]
        public string? BankAccount { get; set; }

        [JsonPropertyName("function")]
        public string? Function { get; set; }

        [JsonPropertyName("type")]
        public SimplicateEmployeeType? Type { get; set; }

        [JsonPropertyName("employment_status")]
        public string? EmploymentStatus { get; set; }

        [JsonPropertyName("civil_status")]
        public SimplicateCivilStatus? CivilStatus { get; set; }

        [JsonPropertyName("work_phone")]
        public string? WorkPhone { get; set; }

        [JsonPropertyName("work_mobile")]
        public string? WorkMobile { get; set; }

        [JsonPropertyName("work_email")]
        public string? WorkEmail { get; set; }

        [JsonPropertyName("hourly_sales_tariff")]
        public string? HourlySalesTariff { get; set; }

        [JsonPropertyName("hourly_cost_tariff")]
        public string? HourlyCostTariff { get; set; }

        [JsonPropertyName("avatar")]
        public SimplicateAvatar? Avatar { get; set; }

        [JsonPropertyName("created_at")]
        public string? CreatedAt { get; set; }

        [JsonPropertyName("updated_at")]
        public string? UpdatedAt { get; set; }

        [JsonPropertyName("simplicate_url")]
        public string? SimplicateUrl { get; set; }
    }

    public class SimplicateEmployeeType
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("label")]
        public string Label { get; set; } = string.Empty;
    }

    public class SimplicateCivilStatus
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("label")]
        public string Label { get; set; } = string.Empty;
    }

    public class SimplicateAvatar
    {
        [JsonPropertyName("url_small")]
        public string? UrlSmall { get; set; }

        [JsonPropertyName("url_large")]
        public string? UrlLarge { get; set; }

        [JsonPropertyName("initials")]
        public string? Initials { get; set; }

        [JsonPropertyName("color")]
        public string? Color { get; set; }
    }
}

