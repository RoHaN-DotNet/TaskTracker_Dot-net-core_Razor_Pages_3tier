using System;
using System.Collections.Generic;
using System.Text;
using TaskTrackerBLL.Common;
using TaskTrackerBLL.DTOs.Reporting;
using TaskTrackerBLL.Interfaces;
using TaskTrackerBLL.Interfaces.Services;
using TaskTrackerDAL.Models.Enums;
using static System.Net.Mime.MediaTypeNames;

namespace TaskTrackerBLL.Services
{
    public class ReportingService : IReportingService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITaskService _taskService;

        public ReportingService(IUnitOfWork unitOfWork, ITaskService taskService)
        {
            _unitOfWork = unitOfWork;
            _taskService = taskService;
        }

        public async Task<Result<ManagerReportDto>> GetManagerReportAsync(int companyId)
        {
            var company = await _unitOfWork.Companies.GetByIdAsync(companyId);
            if (company is null)
            {
                return Result<ManagerReportDto>.Failure($"Company with ID {companyId} was not found.");
            }

            var employeePerformance = await BuildEmployeePerformanceAsync(companyId);
            var projectProgress = await BuildProjectProgressAsync(companyId);

            var completedEntities = await _unitOfWork.Tasks.FilterAsync(
                assignedToUserId: null, priority: null, status: ProjectTasksStatus.Completed,
                projectId: null, companyId: null, enforcedCompanyId: companyId,
                pageNumber: 1, pageSize: 1000);

            var pendingEntities = await _unitOfWork.Tasks.FilterAsync(
                assignedToUserId: null, priority: null, status: ProjectTasksStatus.NotStarted,
                projectId: null, companyId: null, enforcedCompanyId: companyId,
                pageNumber: 1, pageSize: 1000);

            var overdueEntities = await _unitOfWork.Tasks.GetOverdueTasksAsync(companyId);

            var completedTasks = await MapTasksAsync(completedEntities.Items.Select(t => t.Id));
            var pendingTasks = await MapTasksAsync(pendingEntities.Items.Select(t => t.Id));
            var overdueTasks = await MapTasksAsync(overdueEntities.Select(t => t.Id));

            var dto = new ManagerReportDto
            {
                EmployeePerformance = employeePerformance,
                ProjectProgress = projectProgress,
                CompletedTasks = completedTasks,
                PendingTasks = pendingTasks,
                OverDueTasks = overdueTasks
            };

            return Result<ManagerReportDto>.Success(dto);
        }

        public async Task<Result<AdminReportDto>> GetAdminReportAsync()
        {
            var totalCompanies = await _unitOfWork.Companies.CountAsync();
            var totalProjects = await _unitOfWork.Projects.CountAsync();
            var totalTasks = await _unitOfWork.Tasks.CountAsync();
            var completedTasks = await _unitOfWork.Tasks.CountAsync(t => t.Status == ProjectTasksStatus.Completed);
            var pendingTasks = await _unitOfWork.Tasks.CountAsync(t => t.Status == ProjectTasksStatus.NotStarted);
            var overdueTasks = await _unitOfWork.Tasks.CountOverdueAsync(companyId: null);

            var companies = await _unitOfWork.Companies.GetAllAsync();

            var breakdown = new List<CompanyPerformanceSummaryDto>();
            foreach (var company in companies)
            {
                var companyProjects = await _unitOfWork.Projects.CountAsync(p => p.CompanyId == company.Id);
                var companyTotalTasks = await _unitOfWork.Tasks.CountAsync(t => t.Project.CompanyId == company.Id);
                var companyCompletedTasks = await _unitOfWork.Tasks.CountAsync(
                    t => t.Project.CompanyId == company.Id && t.Status == ProjectTasksStatus.Completed);

                breakdown.Add(new CompanyPerformanceSummaryDto
                {
                    CompanyId = company.Id,
                    CompanyName = company.Name,
                    TotalProjects = companyProjects,
                    TotalTasks = companyTotalTasks,
                    CompletedTasks = companyCompletedTasks
                });
            }

            var dto = new AdminReportDto
            {
                TotalCompanies = totalCompanies,
                TotalProjects = totalProjects,
                TotalTasks = totalTasks,
                CompletedTasks = completedTasks,
                PendingTasks = pendingTasks,
                OverdueTasks = overdueTasks,
                CompanyBreakdown = breakdown
            };

            return Result<AdminReportDto>.Success(dto);
        }

        private async Task<IReadOnlyList<EmployeePerformanceDto>> BuildEmployeePerformanceAsync(int companyId)
        {
            var stats = await _unitOfWork.Tasks.GetEmployeePerformanceAsync(companyId);

            var result = new List<EmployeePerformanceDto>();
            foreach (var (userId, totalAssigned, completed) in stats)
            {
                var user = await _unitOfWork.Users.GetByIdAsync(userId);
                var overdueCount = (await _unitOfWork.Tasks.GetOverdueTasksAsync(companyId))
                    .Count(t => t.AssignedToUserId == userId);

                result.Add(new EmployeePerformanceDto
                {
                    UserId = userId,
                    FullName = user?.FullName ?? "Unknown",
                    TotalAssignedTasks = totalAssigned,
                    CompletedTasks = completed,
                    PendingTasks = totalAssigned - completed,
                    OverdueTasks = overdueCount
                });
            }

            return result.OrderByDescending(e => e.CompletionRate).ToList();
        }

        private async Task<IReadOnlyList<ProjectProgressDto>> BuildProjectProgressAsync(int companyId)
        {
            var projects = await _unitOfWork.Projects.GetByCompanyIdAsync(companyId);

            var result = new List<ProjectProgressDto>();
            foreach (var project in projects)
            {
                var totalTasks = await _unitOfWork.Tasks.CountAsync(t => t.ProjectId == project.Id);
                var completedTasks = await _unitOfWork.Tasks.CountAsync(
                    t => t.ProjectId == project.Id && t.Status == ProjectTasksStatus.Completed);

                var percentComplete = totalTasks == 0 ? 0 : completedTasks * 100.0 / totalTasks;

                var isBehindSchedule = project.EndDate.HasValue
                                        && project.EndDate.Value.Date < DateTime.UtcNow.Date
                                        && percentComplete < 100;

                result.Add(new ProjectProgressDto
                {
                    ProjectId = project.Id,
                    ProjectName = project.Name,
                    Status = project.Status,
                    TotalTasks = totalTasks,
                    CompletedTasks = completedTasks,
                    IsBehindSchedule = isBehindSchedule
                });
            }

            return result;
        }

        private async Task<IReadOnlyList<TaskTrackerBLL.DTOs.Task.TaskDto>> MapTasksAsync(IEnumerable<int> taskIds)
        {
            var dtos = new List<TaskTrackerBLL.DTOs.Task.TaskDto>();
            foreach (var id in taskIds)
            {
                var result = await _taskService.GetByIdAsync(id);
                if (result.Succeeded)
                {
                    dtos.Add(result.Value!);
                }
            }
            return dtos;
        }

    }
}
