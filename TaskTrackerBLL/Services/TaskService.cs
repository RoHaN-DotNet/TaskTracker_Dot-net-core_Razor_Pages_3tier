using System;
using System.Collections.Generic;
using System.Text;
using TaskTrackerBLL.Common;
using TaskTrackerBLL.DTOs.Task;
using TaskTrackerBLL.Interfaces;
using TaskTrackerBLL.Interfaces.Services;
using TaskTrackerDAL.Models;
using TaskTrackerDAL.Models.Enums;

namespace TaskTrackerBLL.Services
{
    public class TaskService:ITaskService
    {
        private readonly IUnitOfWork _unitOfWork;

        public TaskService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<TaskDto>> GetByIdAsync(int id)
        {
            var task = await _unitOfWork.Tasks.GetByIdAsync(id);

            if (task is null)
            {
                return Result<TaskDto>.Failure($"Task with ID {id} was not found.");
            }

            var dto = await MapToDtoAsync(task);

            return Result<TaskDto>.Success(dto);
        }

        public async Task<Result<IReadOnlyList<TaskDto>>> GetByProjectIdAsync(int projectId)
        {
            var tasks = await _unitOfWork.Tasks.GetByProjectIdAsync(projectId);

            var dtos = new List<TaskDto>();
            foreach (var task in tasks)
            {
                dtos.Add(await MapToDtoAsync(task));
            }

            return Result<IReadOnlyList<TaskDto>>.Success(dtos);
        }

        public async Task<Result<IReadOnlyList<TaskDto>>> GetByAssignedUserIdAsync(int userId)
        {
            var tasks = await _unitOfWork.Tasks.GetByAssignedUserIdAsync(userId);

            var dtos = new List<TaskDto>();
            foreach (var task in tasks)
            {
                dtos.Add(await MapToDtoAsync(task));
            }

            return Result<IReadOnlyList<TaskDto>>.Success(dtos);
        }

        public async Task<Result<IReadOnlyList<TaskDto>>> GetOverdueTasksAsync(int companyId)
        {
            var tasks = await _unitOfWork.Tasks.GetOverdueTasksAsync(companyId);

            var dtos = new List<TaskDto>();
            foreach (var task in tasks)
            {
                dtos.Add(await MapToDtoAsync(task));
            }

            return Result<IReadOnlyList<TaskDto>>.Success(dtos);
        }

        public async Task<Result<TaskDto>> CreateAsync(CreateTaskDto dto, int createdByUserId)
        {
            var project = await _unitOfWork.Projects.GetByIdAsync(dto.ProjectId);
            if (project is null)
            {
                return Result<TaskDto>.Failure($"Project with ID {dto.ProjectId} was not found.");
            }

            if (dto.AssignedToUserId.HasValue)
            {
                var isMember = await _unitOfWork.Projects.IsUserProjectMemberAsync(
                    dto.ProjectId, dto.AssignedToUserId.Value);

                if (!isMember)
                {
                    return Result<TaskDto>.Failure(
                        "The task can only be assigned to a member of this project's team.");
                }
            }

            var task = new ProjectTask
            {
                ProjectId = dto.ProjectId,
                Title = dto.Title,
                Description = dto.Description,
                AssignedToUserId = dto.AssignedToUserId,
                Status = ProjectTasksStatus.NotStarted,
                Priority = dto.Priority,
                DueDate = dto.DueDate,
                CreatedByUserId = createdByUserId
            };

            await _unitOfWork.Tasks.AddAsync(task);
            await _unitOfWork.SaveChangesAsync();

            var resultDto = await MapToDtoAsync(task);

            return Result<TaskDto>.Success(resultDto);
        }

        public async Task<Result> UpdateAsync(UpdateTaskDto dto)
        {
            var task = await _unitOfWork.Tasks.GetByIdAsync(dto.Id);

            if (task is null)
            {
                return Result.Failure($"Task with ID {dto.Id} was not found.");
            }

            if (dto.AssignedToUserId.HasValue)
            {
                var isMember = await _unitOfWork.Projects.IsUserProjectMemberAsync(
                    task.ProjectId, dto.AssignedToUserId.Value);

                if (!isMember)
                {
                    return Result.Failure(
                        "The task can only be assigned to a member of this project's team.");
                }
            }

            var isTransitioningToDone = dto.Status == ProjectTasksStatus.Completed
                                         && task.Status != ProjectTasksStatus.Completed;

            var isTransitioningAwayFromDone = dto.Status != ProjectTasksStatus.Completed
                                              && task.Status == ProjectTasksStatus.Completed;

            task.Title = dto.Title;
            task.Description = dto.Description;
            task.AssignedToUserId = dto.AssignedToUserId;
            task.Status = dto.Status;
            task.Priority = dto.Priority;
            task.DueDate = dto.DueDate;
            task.UpdatedAt = DateTime.UtcNow;

            if (isTransitioningToDone)
            {
                task.CompletedAt = DateTime.UtcNow;
            }
            else if (isTransitioningAwayFromDone)
            {
                task.CompletedAt = null;
            }

            _unitOfWork.Tasks.Update(task);
            await _unitOfWork.SaveChangesAsync();

            return Result.Success();
        }

        public async Task<Result> DeleteAsync(int id)
        {
            var task = await _unitOfWork.Tasks.GetByIdAsync(id);

            if (task is null)
            {
                return Result.Failure($"Task with ID {id} was not found.");
            }

            _unitOfWork.Tasks.Remove(task);
            await _unitOfWork.SaveChangesAsync();

            return Result.Success();
        }

        private async Task<TaskDto> MapToDtoAsync(ProjectTask task)
        {
            var project = await _unitOfWork.Projects.GetByIdAsync(task.ProjectId);

            string? assignedToUserName = null;
            if (task.AssignedToUserId.HasValue)
            {
                var assignedUser = await _unitOfWork.Users.GetByIdAsync(task.AssignedToUserId.Value);
                assignedToUserName = assignedUser?.FullName;
            }

            var isOverdue = task.DueDate.HasValue
                             && task.DueDate.Value.Date < DateTime.UtcNow.Date
                             && task.Status != ProjectTasksStatus.Completed;

            return new TaskDto
            {
                Id = task.Id,
                ProjectId = task.ProjectId,
                ProjectName = project?.Name ?? "Unknown",
                Title = task.Title,
                Description = task.Description,
                AssignedToUserId = task.AssignedToUserId,
                AssignedToUserName = assignedToUserName,
                Status = task.Status,
                Priority = task.Priority,
                DueDate = task.DueDate,
                IsOverdue = isOverdue,
                CompletedAt = task.CompletedAt,
                CreatedAt = task.CreatedAt
            };
        }
    }
}
