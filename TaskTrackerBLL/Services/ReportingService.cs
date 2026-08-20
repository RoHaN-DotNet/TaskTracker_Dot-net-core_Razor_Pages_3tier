using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using TaskTrackerBLL.Common;
using TaskTrackerBLL.DTOs.Reporting;
using TaskTrackerBLL.DTOs.Tasks;
using TaskTrackerBLL.Interfaces;
using TaskTrackerBLL.Interfaces.Services;

using TaskTrackerDAL.Models.Enums;

namespace TaskTrackerBLL.Services
{
    public class ReportingService : IReportingService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITaskService _taskService;

        public ReportingService(
            IUnitOfWork unitOfWork,
            ITaskService taskService)
        {
            _unitOfWork = unitOfWork;
            _taskService = taskService;
        }

        // =========================================================
        // MANAGER REPORT
        // =========================================================

        public async Task<Result<ManagerReportDto>>
            GetManagerReportAsync(int companyId)
        {
            // -----------------------------------------------------
            // Check company
            // -----------------------------------------------------

            var company =
                await _unitOfWork.Companies
                    .GetByIdAsync(companyId);

            if (company is null)
            {
                return Result<ManagerReportDto>.Failure(
                    $"Company with ID {companyId} was not found.");
            }

            // -----------------------------------------------------
            // Employee Performance
            // -----------------------------------------------------

            var employeePerformance =
                await BuildEmployeePerformanceAsync(companyId);

            // -----------------------------------------------------
            // Project Progress
            // -----------------------------------------------------

            var projectProgress =
                await BuildProjectProgressAsync(companyId);

            // -----------------------------------------------------
            // Completed Tasks
            // -----------------------------------------------------

            var completedResult =
                await _unitOfWork.Tasks.FilterAsync(
                    assignedToUserId: null,
                    priority: null,
                    status: ProjectTasksStatus.Completed,
                    projectId: null,
                    companyId: null,
                    enforcedCompanyId: companyId,
                    pageNumber: 1,
                    pageSize: 1000);

            // -----------------------------------------------------
            // Pending Tasks
            // -----------------------------------------------------

            var pendingResult =
                await _unitOfWork.Tasks.FilterAsync(
                    assignedToUserId: null,
                    priority: null,
                    status: ProjectTasksStatus.NotStarted,
                    projectId: null,
                    companyId: null,
                    enforcedCompanyId: companyId,
                    pageNumber: 1,
                    pageSize: 1000);

            // -----------------------------------------------------
            // Overdue Tasks
            // -----------------------------------------------------

            var overdueEntities =
                await _unitOfWork.Tasks
                    .GetOverdueTasksAsync(companyId);

            // -----------------------------------------------------
            // Map Tasks
            // -----------------------------------------------------

            var completedTasks =
                await MapTasksAsync(
                    completedResult.Items.Select(t => t.Id));

            var pendingTasks =
                await MapTasksAsync(
                    pendingResult.Items.Select(t => t.Id));

            var overdueTasks =
                await MapTasksAsync(
                    overdueEntities.Select(t => t.Id));

            // -----------------------------------------------------
            // Build DTO
            // -----------------------------------------------------

            var dto = new ManagerReportDto
            {
                EmployeePerformance =
                    employeePerformance,

                ProjectProgress =
                    projectProgress,

                CompletedTasks =
                    completedTasks,

                PendingTasks =
                    pendingTasks,

                OverDueTasks =
                    overdueTasks
            };

            return Result<ManagerReportDto>.Success(dto);
        }


        // =========================================================
        // ADMIN REPORT
        // =========================================================

        public async Task<Result<AdminReportDto>>
            GetAdminReportAsync()
        {
            // -----------------------------------------------------
            // Overall Counts
            // -----------------------------------------------------

            var totalCompanies =
                await _unitOfWork.Companies.CountAsync();

            var totalProjects =
                await _unitOfWork.Projects.CountAsync();

            var totalTasks =
                await _unitOfWork.Tasks.CountAsync();

            var completedTasks =
                await _unitOfWork.Tasks.CountAsync(
                    t =>
                        t.Status ==
                        ProjectTasksStatus.Completed);

            var pendingTasks =
                await _unitOfWork.Tasks.CountAsync(
                    t =>
                        t.Status ==
                        ProjectTasksStatus.NotStarted);

            var overdueTasks =
                await _unitOfWork.Tasks
                    .CountOverdueAsync(null);

            // -----------------------------------------------------
            // Company Breakdown
            // -----------------------------------------------------

            var companies =
                await _unitOfWork.Companies
                    .GetAllAsync();

            var breakdown =
                new List<CompanyPerformanceSummaryDto>();

            foreach (var company in companies)
            {
                var companyProjects =
                    await _unitOfWork.Projects.CountAsync(
                        p =>
                            p.CompanyId ==
                            company.Id);

                var companyTotalTasks =
                    await _unitOfWork.Tasks.CountAsync(
                        t =>
                            t.Project.CompanyId ==
                            company.Id);

                var companyCompletedTasks =
                    await _unitOfWork.Tasks.CountAsync(
                        t =>
                            t.Project.CompanyId ==
                            company.Id &&
                            t.Status ==
                            ProjectTasksStatus.Completed);

                breakdown.Add(
                    new CompanyPerformanceSummaryDto
                    {
                        CompanyId =
                            company.Id,

                        CompanyName =
                            company.Name,

                        TotalProjects =
                            companyProjects,

                        TotalTasks =
                            companyTotalTasks,

                        CompletedTasks =
                            companyCompletedTasks
                    });
            }

            // -----------------------------------------------------
            // Build Admin DTO
            // -----------------------------------------------------

            var dto = new AdminReportDto
            {
                TotalCompanies =
                    totalCompanies,

                TotalProjects =
                    totalProjects,

                TotalTasks =
                    totalTasks,

                CompletedTasks =
                    completedTasks,

                PendingTasks =
                    pendingTasks,

                OverdueTasks =
                    overdueTasks,

                CompanyBreakdown =
                    breakdown
            };

            return Result<AdminReportDto>.Success(dto);
        }


        // =========================================================
        // EMPLOYEE PERFORMANCE
        // =========================================================

        private async Task<IReadOnlyList<EmployeePerformanceDto>>
            BuildEmployeePerformanceAsync(int companyId)
        {
            // -----------------------------------------------------
            // Get employee statistics
            // -----------------------------------------------------

            var stats =
                await _unitOfWork.Tasks
                    .GetEmployeePerformanceAsync(
                        companyId);

            // -----------------------------------------------------
            // Get overdue count per employee
            // -----------------------------------------------------

            var overdueCounts =
                await _unitOfWork.Tasks
                    .GetOverdueTaskCountsByUserAsync(
                        companyId);

            var result =
                new List<EmployeePerformanceDto>();

            // -----------------------------------------------------
            // Build employee performance
            // -----------------------------------------------------

            foreach (var stat in stats)
            {
                var userId =
                    stat.UserId;

                var totalAssigned =
                    stat.TotalAssigned;

                var completed =
                    stat.Completed;

                // -------------------------------------------------
                // Get employee
                // -------------------------------------------------

                var user =
                    await _unitOfWork.Users
                        .GetByIdAsync(userId);

                // -------------------------------------------------
                // Get overdue count
                // -------------------------------------------------

                var overdueCount =
                    overdueCounts.TryGetValue(
                        userId,
                        out var count)
                        ? count
                        : 0;

                // -------------------------------------------------
                // Add DTO
                // -------------------------------------------------

                result.Add(
                    new EmployeePerformanceDto
                    {
                        UserId =
                            userId,

                        FullName =
                            user?.FullName ??
                            "Unknown",

                        TotalAssignedTasks =
                            totalAssigned,

                        CompletedTasks =
                            completed,

                        PendingTasks =
                            totalAssigned -
                            completed,

                        OverdueTasks =
                            overdueCount
                    });
            }

            // -----------------------------------------------------
            // Sort by completion rate
            // -----------------------------------------------------

            return result
                .OrderByDescending(
                    e => e.CompletionRate)
                .ToList();
        }


        // =========================================================
        // PROJECT PROGRESS
        // =========================================================

        private async Task<IReadOnlyList<ProjectProgressDto>>
            BuildProjectProgressAsync(int companyId)
        {
            var projects =
                await _unitOfWork.Projects
                    .GetByCompanyIdAsync(
                        companyId);

            var result =
                new List<ProjectProgressDto>();

            foreach (var project in projects)
            {
                // -------------------------------------------------
                // Total tasks
                // -------------------------------------------------

                var totalTasks =
                    await _unitOfWork.Tasks.CountAsync(
                        t =>
                            t.ProjectId ==
                            project.Id);

                // -------------------------------------------------
                // Completed tasks
                // -------------------------------------------------

                var completedTasks =
                    await _unitOfWork.Tasks.CountAsync(
                        t =>
                            t.ProjectId ==
                            project.Id &&
                            t.Status ==
                            ProjectTasksStatus.Completed);

                // -------------------------------------------------
                // Completion percentage
                // -------------------------------------------------

                double percentComplete =
                    totalTasks == 0
                        ? 0
                        : completedTasks *
                          100.0 /
                          totalTasks;

                // -------------------------------------------------
                // Behind schedule
                // -------------------------------------------------

                bool isBehindSchedule =
                    project.EndDate.HasValue &&
                    project.EndDate.Value.Date <
                    DateTime.UtcNow.Date &&
                    percentComplete < 100;

                // -------------------------------------------------
                // Add DTO
                // -------------------------------------------------

                result.Add(
                    new ProjectProgressDto
                    {
                        ProjectId =
                            project.Id,

                        ProjectName =
                            project.Name,

                        Status =
                            project.Status,

                        TotalTasks =
                            totalTasks,

                        CompletedTasks =
                            completedTasks,

                        IsBehindSchedule =
                            isBehindSchedule
                    });
            }

            return result;
        }


        // =========================================================
        // MAP TASKS
        // =========================================================

        private async Task<IReadOnlyList<TaskDto>>
            MapTasksAsync(
                IEnumerable<int> taskIds)
        {
            var dtos =
                new List<TaskDto>();

            foreach (var taskId in
                     taskIds.Distinct())
            {
                var result =
                    await _taskService
                        .GetByIdAsync(taskId);

                if (result.Succeeded &&
                    result.Value is not null)
                {
                    dtos.Add(
                        result.Value);
                }
            }

            return dtos;
        }
    }
}