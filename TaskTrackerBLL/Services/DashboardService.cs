using System;
using System.Collections.Generic;
using System.Text;
using TaskTrackerBLL.Common;
using TaskTrackerBLL.DTOs.Dashboard;
using TaskTrackerBLL.Interfaces;
using TaskTrackerBLL.Interfaces.Services;
using TaskTrackerDAL.Models.Enums;

namespace TaskTrackerBLL.Services
{
    public class DashboardService: IDashboardService
    {
        private readonly IUnitOfWork _unitOfWork;

        public DashboardService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

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
    }
}
