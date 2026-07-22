using System;
using System.Collections.Generic;
using System.Text;
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
                OverdueTasks = overdueTasks
            };

            return Result<AdminDashboardDto>.Success(dto);
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

            var dueToday = new List<TaskTrackerBLL.DTOs.Task.TaskDto>();
            foreach (var task in dueTodayEntities)
            {
                var taskResult = await _taskService.GetByIdAsync(task.Id);
                if (taskResult.Succeeded)
                {
                    dueToday.Add(taskResult.Value!);
                }

            }
            var recent = new List<TaskTrackerBLL.DTOs.Task.TaskDto>();
            foreach(var task in recentEntities)
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
                MyEmployees=employees,
                MyProjects=projects,
                TodaysDeadline=dueToday,
                RecentTasks=recent
            };
            return Result<ManagerDashboardDto>.Success(dto);
        }
        public async Task<Result<EmployeeDashboardDto>> GetEmployeeDashboardAsync(int userId)
        {
            var projectsResult = await _projectService.GetByMemberUserIdAsync(userId);
            var projects = projectsResult.Succeeded ? projectsResult.Value! : Array.Empty<TaskTrackerBLL.DTOs.Project.ProjectDto>();

            var tasksResult = await _taskService.GetByAssignedUserIdAsync(userId);
            var allTasks = tasksResult.Succeeded ? tasksResult.Value! : Array.Empty<TaskTrackerBLL.DTOs.Task.TaskDto>();

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
