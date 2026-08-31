using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using TaskTrackerBLL.Common;
using TaskTrackerBLL.DTOs.Dashboard;
using TaskTrackerBLL.DTOs.Employee;
using TaskTrackerBLL.Interfaces;
using TaskTrackerBLL.Interfaces.Services;
using TaskTrackerDAL.Constants;
using TaskTrackerDAL.Models.Enums;
using static System.Net.Mime.MediaTypeNames;

namespace TaskTrackerBLL.Services
{
    public class DashboardService : IDashboardService
    {
        // Valid values for the time-range dropdown on the admin dashboard.
        private const string Range7Days = "7d";
        private const string Range30Days = "30d";
        private const string Range6Months = "6m";
        private const string Range12Months = "12m";
        private const string DefaultRange = Range12Months;

        private readonly IUnitOfWork _unitOfWork;
        private readonly IProjectService _projectService;
        private readonly ITaskService _taskService;
        private readonly IEmployeeService _employeeService;
        private readonly ICompanyService _companyService;

        public DashboardService(IUnitOfWork unitOfWork, IProjectService projectService, ITaskService taskService, IEmployeeService employeeService, ICompanyService companyService)
        {
            _unitOfWork = unitOfWork;
            _projectService = projectService;
            _taskService = taskService;
            _employeeService = employeeService;
            _companyService = companyService;
        }
        public async Task<Result<AdminDashboardDto>> GetAdminDashboardAsync()
        {
            var totalCompanies = await _unitOfWork.Companies.CountAsync();
            var totalUsers = await _unitOfWork.Users.CountAsync();
            var totalManagers = await _unitOfWork.Users.CountByRoleNamesAsync(new[] { AppRoles.Manager });
            var totalEmployees = await _unitOfWork.Users.CountByRoleNamesAsync(AppRoles.EmployeeRoles);
            var totalProjects = await _unitOfWork.Projects.CountAsync();
            var totalTasks = await _unitOfWork.Tasks.CountAsync();
            var completedTasks = await _unitOfWork.Tasks.CountAsync(t => t.Status == ProjectTasksStatus.Completed);
            var pendingTasks = await _unitOfWork.Tasks.CountAsync(t => t.Status == ProjectTasksStatus.NotStarted);
            var overdueTasks = await _unitOfWork.Tasks.CountOverdueAsync(companyId: null);

            // Growth trends for the default range, so the page has data to
            // render on first load (before the user touches the dropdown).
            var trendsResult = await GetAdminDashboardTrendsAsync(DefaultRange);
            var trends = trendsResult.Succeeded
                ? trendsResult.Value!
                : new DashboardTrendDto { Range = DefaultRange };

            var usersByRole = await BuildUsersByRoleAsync();
            var projectsByStatus = await BuildProjectsByStatusAsync();
            var tasksByStatus = await BuildTasksByStatusAsync();

            var dto = new AdminDashboardDto
            {
                TotalCompanies = totalCompanies,
                TotalUsers = totalUsers,
                TotalManagers = totalManagers,
                TotalEmployees = totalEmployees,
                TotalProjects = totalProjects,
                TotalTasks = totalTasks,
                CompletedTasks = completedTasks,
                PendingTasks = pendingTasks,
                OverdueTasks = overdueTasks,

                TrendRange = trends.Range,
                CompanyGrowth = trends.CompanyGrowth,
                UserGrowth = trends.UserGrowth,
                ProjectGrowth = trends.ProjectGrowth,
                TaskGrowth = trends.TaskGrowth,

                UsersByRole = usersByRole,
                ProjectsByStatus = projectsByStatus,
                TasksByStatus = tasksByStatus
            };

            return Result<AdminDashboardDto>.Success(dto);
        }

        public async Task<Result<DashboardTrendDto>> GetAdminDashboardTrendsAsync(string range)
        {
            var normalizedRange = NormalizeRange(range);

            var companies = await _unitOfWork.Companies.GetAllAsync();
            var users = await _unitOfWork.Users.GetAllAsync();
            var projects = await _unitOfWork.Projects.GetAllAsync();
            var tasks = await _unitOfWork.Tasks.GetAllAsync();

            var dto = new DashboardTrendDto
            {
                Range = normalizedRange,
                CompanyGrowth = BuildTrend(companies.Select(c => c.CreatedAt), normalizedRange),
                UserGrowth = BuildTrend(users.Select(u => u.CreatedAt), normalizedRange),
                ProjectGrowth = BuildTrend(projects.Select(p => p.CreatedAt), normalizedRange),
                TaskGrowth = BuildTrend(tasks.Select(t => t.CreatedAt), normalizedRange)
            };

            return Result<DashboardTrendDto>.Success(dto);
        }

        // =====================================================
        // TREND / BREAKDOWN HELPERS
        // =====================================================

        private static string NormalizeRange(string? range)
        {
            return range switch
            {
                Range7Days => Range7Days,
                Range30Days => Range30Days,
                Range6Months => Range6Months,
                Range12Months => Range12Months,
                _ => DefaultRange
            };
        }

        /// <summary>
        /// Groups a set of creation dates into evenly-spaced buckets for the
        /// requested range, filling any bucket with no records with 0.
        /// Daily buckets (7d/30d) are keyed by full date (year+month+day).
        /// Monthly buckets (6m/12m) are keyed by year+month, so e.g.
        /// January 2025 and January 2026 are never combined into one bucket.
        /// </summary>
        private static List<TrendPointDto> BuildTrend(IEnumerable<DateTime> createdDates, string normalizedRange)
        {
            var dates = createdDates.ToList();

            switch (normalizedRange)
            {
                case Range7Days:
                    return BuildDailyTrend(dates, days: 7);

                case Range30Days:
                    return BuildDailyTrend(dates, days: 30);

                case Range6Months:
                    return BuildMonthlyTrend(dates, months: 6);

                case Range12Months:
                default:
                    return BuildMonthlyTrend(dates, months: 12);
            }
        }

        private static List<TrendPointDto> BuildDailyTrend(List<DateTime> dates, int days)
        {
            var endDate = DateTime.UtcNow.Date;
            var startDate = endDate.AddDays(-(days - 1));

            var buckets = Enumerable.Range(0, days)
                .Select(offset => startDate.AddDays(offset))
                .ToList();

            var counts = buckets.ToDictionary(bucket => bucket, _ => 0);

            foreach (var date in dates)
            {
                var day = date.Date;

                if (day >= startDate && day <= endDate && counts.ContainsKey(day))
                {
                    counts[day]++;
                }
            }

            return buckets
                .Select(bucket => new TrendPointDto
                {
                    Label = bucket.ToString("MMM dd"),
                    Count = counts[bucket]
                })
                .ToList();
        }

        private static List<TrendPointDto> BuildMonthlyTrend(List<DateTime> dates, int months)
        {
            var endMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
            var startMonth = endMonth.AddMonths(-(months - 1));

            var buckets = Enumerable.Range(0, months)
                .Select(offset => startMonth.AddMonths(offset))
                .ToList();

            // Keyed by year+month (not just month number) so different
            // years are never combined into the same bucket.
            var counts = buckets.ToDictionary(bucket => bucket, _ => 0);

            foreach (var date in dates)
            {
                var monthKey = new DateTime(date.Year, date.Month, 1);

                if (monthKey >= startMonth && monthKey <= endMonth && counts.ContainsKey(monthKey))
                {
                    counts[monthKey]++;
                }
            }

            return buckets
                .Select(bucket => new TrendPointDto
                {
                    Label = bucket.ToString("MMM"),
                    Count = counts[bucket]
                })
                .ToList();
        }

        private async Task<List<TrendPointDto>> BuildUsersByRoleAsync()
        {
            var roles = await _unitOfWork.Roles.GetAllAsync();
            var userRoles = await _unitOfWork.UserRoles.GetAllAsync();

            var countsByRoleId = userRoles
                .GroupBy(ur => ur.RoleId)
                .ToDictionary(g => g.Key, g => g.Count());

            return roles
                .Select(role => new TrendPointDto
                {
                    Label = role.Name,
                    Count = countsByRoleId.TryGetValue(role.Id, out var count) ? count : 0
                })
                .Where(point => point.Count > 0)
                .OrderByDescending(point => point.Count)
                .ToList();
        }

        private async Task<List<TrendPointDto>> BuildProjectsByStatusAsync()
        {
            var projects = await _unitOfWork.Projects.GetAllAsync();

            var countsByStatus = projects
                .GroupBy(p => p.Status)
                .ToDictionary(g => g.Key, g => g.Count());

            return Enum.GetValues<ProjectStatus>()
                .Select(status => new TrendPointDto
                {
                    Label = HumanizeEnumName(status.ToString()),
                    Count = countsByStatus.TryGetValue(status, out var count) ? count : 0
                })
                .Where(point => point.Count > 0)
                .ToList();
        }

        private async Task<List<TrendPointDto>> BuildTasksByStatusAsync()
        {
            var tasks = await _unitOfWork.Tasks.GetAllAsync();

            var countsByStatus = tasks
                .GroupBy(t => t.Status)
                .ToDictionary(g => g.Key, g => g.Count());

            return Enum.GetValues<ProjectTasksStatus>()
                .Select(status => new TrendPointDto
                {
                    Label = HumanizeEnumName(status.ToString()),
                    Count = countsByStatus.TryGetValue(status, out var count) ? count : 0
                })
                .Where(point => point.Count > 0)
                .ToList();
        }

        /// <summary>Turns "NotStarted" into "Not Started" for chart labels.</summary>
        private static string HumanizeEnumName(string enumName)
        {
            return Regex.Replace(enumName, "(\\B[A-Z])", " $1");
        }
        /*
        public async Task<Result<DashboardDto>> GetDashboardAsync(int companyId)
        {
            var company = await _unitOfWork.Companies.GetByIdAsync(companyId);
            if (company is null)
            {
                return Result<DashboardDto>.Failure($"Company with ID {companyId} was not found.");
            }

            var totalProjects = await _unitOfWork.Projects.CountAsync(p => p.CompanyId == companyId);
            var activeProjects = await _unitOfWork.Projects.CountByStatusAsync(companyId, ProjectStatus.InProgress);
            var completedProjects = await _unitOfWork.Projects.CountByStatusAsync(companyId, ProjectStatus.Completed);

            var completedTasks = await _unitOfWork.Tasks.CountByStatusAsync(companyId, ProjectTasksStatus.Completed);
            var overdueTasks = await _unitOfWork.Tasks.GetOverdueTasksAsync(companyId);

            var openToDo = await _unitOfWork.Tasks.CountByStatusAsync(companyId, ProjectTasksStatus.NotStarted);
            var openInProgress = await _unitOfWork.Tasks.CountByStatusAsync(companyId, ProjectTasksStatus.InProgress);
            var openInReview = await _unitOfWork.Tasks.CountByStatusAsync(companyId, ProjectTasksStatus.OnHold);
            var totalOpenTasks = openToDo + openInProgress + openInReview;

            var workloadByUserId = await _unitOfWork.Tasks.GetOpenTaskCountsByUserAsync(companyId);

            var workloadByUserName = new Dictionary<string, int>();
            foreach (var (userId, count) in workloadByUserId)
            {
                var user = await _unitOfWork.Users.GetByIdAsync(userId);
                workloadByUserName[user?.FullName ?? $"User #{userId}"] = count;
            }

            var dto = new DashboardDto
            {
                TotalProjects = totalProjects,
                ActiveProjects = activeProjects,
                CompletedProjects = completedProjects,
                TotalOpenTasks = totalOpenTasks,
                TotalCompletedTasks = completedTasks,
                OverdueTaskCount = overdueTasks.Count,
                OpenTaskCountsByUser = workloadByUserName
            };

            return Result<DashboardDto>.Success(dto);
        }
        */

        public async Task<Result<ManagerDashboardDto>> GetManagerDashboardAsync(int companyId)
        {
            var companyResult = await _companyService.GetByIdAsync(companyId);
            if (!companyResult.Succeeded)
            {
                return Result<ManagerDashboardDto>.Failure($"Company with ID {companyId} was not found.");
            }
            var employeesResult = await _employeeService.SearchAsync(new EmployeeSearchFilterDto(), companyId);
            var employees = employeesResult.Succeeded ? employeesResult.Value! : new List<EmployeeDto>();

            var projectsResult = await _projectService.SearchAsync(
                new TaskTrackerBLL.DTOs.Project.ProjectSearchFilterDto { PageSize = 100 }, companyId);

            var projects = projectsResult.Succeeded ? projectsResult.Value!.Items : Array.Empty<TaskTrackerBLL.DTOs.Project.ProjectDto>();

            var dueTodayEntities = await _unitOfWork.Tasks.GetDueTodayAsync(companyId, userId: null);
            var recentEntities = await _unitOfWork.Tasks.GetRecentAsync(companyId, count: 10);

            var dueToday = new List<TaskTrackerBLL.DTOs.Tasks.TaskDto>();
            foreach (var task in dueTodayEntities)
            {
                var taskResult = await _taskService.GetByIdAsync(task.Id);
                if (taskResult.Succeeded)
                {
                    dueToday.Add(taskResult.Value!);
                }

            }
            var recent = new List<TaskTrackerBLL.DTOs.Tasks.TaskDto>();
            foreach (var task in recentEntities)
            {
                var taskResult = await _taskService.GetByIdAsync(task.Id);
                if (taskResult.Succeeded)
                {
                    recent.Add(taskResult.Value!);
                }
            }
            var dto = new ManagerDashboardDto
            {
                MyCompany = companyResult.Value!,
                MyEmployees = employees,
                MyProjects = projects,
                TodaysDeadline = dueToday,
                RecentTasks = recent
            };
            return Result<ManagerDashboardDto>.Success(dto);
        }
        public async Task<Result<EmployeeDashboardDto>> GetEmployeeDashboardAsync(int userId)
        {
            var projectsResult = await _projectService.GetByMemberUserIdAsync(userId);
            var projects = projectsResult.Succeeded ? projectsResult.Value! : Array.Empty<TaskTrackerBLL.DTOs.Project.ProjectDto>();

            var tasksResult = await _taskService.GetByAssignedUserIdAsync(userId);
            var allTasks = tasksResult.Succeeded ? tasksResult.Value! : Array.Empty<TaskTrackerBLL.DTOs.Tasks.TaskDto>();

            var completedTasks = allTasks.Where(t => t.Status == ProjectTasksStatus.Completed).ToList();
            var pendingTasks = allTasks.Where(t => t.Status == ProjectTasksStatus.NotStarted).ToList();
            var todaysDeadlines = allTasks.Where(t => t.DueDate.HasValue && t.DueDate.Value.Date == DateTime.UtcNow.Date).ToList();

            var dto = new EmployeeDashboardDto
            {
                AssignedProjects = projects,
                AssignedTasks = allTasks,
                CompletedTasks = completedTasks,
                PendingTasks = pendingTasks,
                TodaysDeadlines = todaysDeadlines
            };

            return Result<EmployeeDashboardDto>.Success(dto);
        }


    }
}
