using System;
using System.Collections.Generic;
using System.Text;
using TaskTrackerBLL.Common;

using TaskTrackerBLL.DTOs.Tasks;
using TaskTrackerBLL.Interfaces;
using TaskTrackerBLL.Interfaces.Services;
using TaskTrackerDAL.Models;
using TaskTrackerDAL.Models.Enums;

namespace TaskTrackerBLL.Services
{
    public class TaskService:ITaskService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;

        public TaskService(IUnitOfWork unitOfWork, IAuditService auditService)
        {
            _unitOfWork = unitOfWork;
            _auditService = auditService;
        }

        public async Task<Result<TaskDto>> GetByIdAsync(int id)
        {
            var task = await _unitOfWork.Tasks.GetByIdAsync(id);

            if (task is null)
            {
                return Result<TaskDto>.Failure($"Task with ID {id} was not found.");
            }

            return Result<TaskDto>.Success(await MapToDtoAsync(task));
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

        public async Task<Result<TaskDto>> CreateAsync(
            CreateTaskDto dto, int createdByUserId, int? actingManagerCompanyId)
        {
            var project = await _unitOfWork.Projects.GetByIdAsync(dto.ProjectId);
            if (project is null)
            {
                return Result<TaskDto>.Failure($"Project with ID {dto.ProjectId} was not found.");
            }

            if (actingManagerCompanyId.HasValue && project.CompanyId != actingManagerCompanyId.Value)
            {
                return Result<TaskDto>.Failure("You are not authorized to create tasks for this project.");
            }
            /*
            if (dto.AssignedToUserId.HasValue)
            {
                var isMember = await _unitOfWork.Projects.IsUserProjectMemberAsync(
                    dto.ProjectId, dto.AssignedToUserId.Value);

                if (!isMember)
                {
                    return Result<TaskDto>.Failure(
                        "The task can only be assigned to a member of this project's team.");
                }
            }*/

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

            return Result<TaskDto>.Success(await MapToDtoAsync(task));
        }

        public async Task<Result> DeleteAsync(int id, int actingUserId, int? actingManagerCompanyId)
        {
            var task = await _unitOfWork.Tasks.GetByIdAsync(id);
            if (task is null)
            {
                return Result.Failure($"Task with ID {id} was not found.");
            }

            var scopeResult = await ValidateManagerScopeAsync(task, actingManagerCompanyId);
            if (!scopeResult.Succeeded) return scopeResult;

            var snapshot = $"{task.Title} (Project #{task.ProjectId})";

            _unitOfWork.Tasks.Remove(task);
            await _unitOfWork.SaveChangesAsync();

            await _auditService.LogDeletedAsync("ProjectTask", id, actingUserId, snapshot);

            return Result.Success();
        }

        public async Task<Result> AssignAsync(AssignTaskDto dto, int? actingManagerCompanyId)
        {
            var task = await _unitOfWork.Tasks.GetByIdAsync(dto.TaskId);
            if (task is null)
            {
                return Result.Failure($"Task with ID {dto.TaskId} was not found.");
            }

            var scopeResult = await ValidateManagerScopeAsync(task, actingManagerCompanyId);
            if (!scopeResult.Succeeded)
            {
                return scopeResult;
            }

            if (task.AssignedToUserId.HasValue)
            {
                return Result.Failure(
                    "This task is already assigned. Use Reassign to change the assignee.");
            }

            return await SetAssigneeAsync(task, dto.AssignedToUserId);
        }

        public async Task<Result> ReassignAsync(AssignTaskDto dto, int? actingManagerCompanyId)
        {
            var task = await _unitOfWork.Tasks.GetByIdAsync(dto.TaskId);
            if (task is null)
            {
                return Result.Failure($"Task with ID {dto.TaskId} was not found.");
            }

            var scopeResult = await ValidateManagerScopeAsync(task, actingManagerCompanyId);
            if (!scopeResult.Succeeded)
            {
                return scopeResult;
            }

            if (!task.AssignedToUserId.HasValue)
            {
                return Result.Failure("This task has no current assignee. Use Assign instead.");
            }

            if (task.AssignedToUserId.Value == dto.AssignedToUserId)
            {
                return Result.Failure("This task is already assigned to that employee.");
            }

            return await SetAssigneeAsync(task, dto.AssignedToUserId);
        }

        public async Task<Result> ChangeDeadlineAsync(ChangeDeadlineDto dto, int? actingManagerCompanyId)
        {
            var task = await _unitOfWork.Tasks.GetByIdAsync(dto.TaskId);
            if (task is null)
            {
                return Result.Failure($"Task with ID {dto.TaskId} was not found.");
            }

            var scopeResult = await ValidateManagerScopeAsync(task, actingManagerCompanyId);
            if (!scopeResult.Succeeded)
            {
                return scopeResult;
            }

            task.DueDate = dto.NewDueDate;
            task.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Tasks.Update(task);
            await _unitOfWork.SaveChangesAsync();

            return Result.Success();
        }

        public async Task<Result> ChangePriorityAsync(ChangePriorityDto dto, int? actingManagerCompanyId)
        {
            var task = await _unitOfWork.Tasks.GetByIdAsync(dto.TaskId);
            if (task is null)
            {
                return Result.Failure($"Task with ID {dto.TaskId} was not found.");
            }

            var scopeResult = await ValidateManagerScopeAsync(task, actingManagerCompanyId);
            if (!scopeResult.Succeeded)
            {
                return scopeResult;
            }

            task.Priority = dto.NewPriority;
            task.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Tasks.Update(task);
            await _unitOfWork.SaveChangesAsync();

            return Result.Success();
        }

        public async Task<Result> ChangeStatusAsync(MoveTaskStatusDto dto, int actingUserId,bool isManager)
        {
            var task = await _unitOfWork.Tasks.GetByIdAsync(dto.TaskId);
            
            if (task == null)
            {
                return Result.Failure($"Task with ID {dto.TaskId} was not found.");
            }
            if (!isManager)
            {
                if (task.Status == ProjectTasksStatus.Completed || task.Status == ProjectTasksStatus.Cancelled)
                {
                    return Result.Failure($"This task is already {task.Status} and its status can no longer be changed.");
                }
                if (task.AssignedToUserId != actingUserId)
                {
                    return Result.Failure("You can only update the status of tasks assigned to you.");
                }
            }
            
            

            

            task.Status = dto.NewStatus;
            task.UpdatedAt = DateTime.UtcNow;

            if (dto.NewStatus == ProjectTasksStatus.Completed)
            {
                task.CompletedAt = DateTime.UtcNow;
            }

            _unitOfWork.Tasks.Update(task);
            await _unitOfWork.SaveChangesAsync();

            return Result.Success();
        }

        public async Task<Result<IReadOnlyList<TaskProgressNoteDto>>> GetProgressNotesAsync(int taskId)
        {
            var notes = await _unitOfWork.Tasks.GetProgressNotesAsync(taskId);

            var dtos = notes.Select(n => new TaskProgressNoteDto
            {
                Id = n.Id,
                AuthorName = n.AuthorUser.FullName,
                Note = n.Note,
                IsCompletionComment = n.IsCompletionComment,
                CreatedAt = n.CreatedAt
            }).ToList();

            return Result<IReadOnlyList<TaskProgressNoteDto>>.Success(dtos);
        }

        public async Task<Result> AddProgressNoteAsync(AddProgressNoteDto dto, int authorUserId)
        {
            var task = await _unitOfWork.Tasks.GetByIdAsync(dto.TaskId);
            if (task is null)
            {
                return Result.Failure($"Task with ID {dto.TaskId} was not found.");
            }

            if (task.AssignedToUserId != authorUserId)
            {
                return Result.Failure("You can only add notes to tasks assigned to you.");
            }

            var note = new TaskProgressNote
            {
                TaskId = dto.TaskId,
                AuthorUserId = authorUserId,
                Note = dto.Note,
                IsCompletionComment = false
            };

            await _unitOfWork.Tasks.AddProgressNoteAsync(note);
            await _unitOfWork.SaveChangesAsync();

            return Result.Success();
        }

        public async Task<Result> AddCompletionCommentAsync(AddCompletionCommentDto dto, int authorUserId)
        {
            var task = await _unitOfWork.Tasks.GetByIdAsync(dto.TaskId);
            if (task is null)
            {
                return Result.Failure($"Task with ID {dto.TaskId} was not found.");
            }

            if (task.AssignedToUserId != authorUserId)
            {
                return Result.Failure("You can only comment on tasks assigned to you.");
            }

            if (task.Status != ProjectTasksStatus.Completed)
            {
                return Result.Failure("A completion comment can only be added once the task is marked Completed.");
            }

            var note = new TaskProgressNote
            {
                TaskId = dto.TaskId,
                AuthorUserId = authorUserId,
                Note = dto.Comment,
                IsCompletionComment = true
            };

            await _unitOfWork.Tasks.AddProgressNoteAsync(note);
            await _unitOfWork.SaveChangesAsync();

            return Result.Success();
        }

        private async Task<Result> ValidateManagerScopeAsync(ProjectTask task, int? actingManagerCompanyId)
        {
            if (!actingManagerCompanyId.HasValue)
            {
                return Result.Success();
            }

            var project = await _unitOfWork.Projects.GetByIdAsync(task.ProjectId);

            if (project is null || project.CompanyId != actingManagerCompanyId.Value)
            {
                return Result.Failure("You are not authorized to manage this task.");
            }

            return Result.Success();
        }

        private async Task<Result> SetAssigneeAsync(ProjectTask task, int newAssignedToUserId)
        {
            var isMember = await _unitOfWork.Projects.IsUserProjectMemberAsync(task.ProjectId, newAssignedToUserId);

            if (!isMember)
            {
                return Result.Failure("The task can only be assigned to a member of this project's team.");
            }

            task.AssignedToUserId = newAssignedToUserId;
            task.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Tasks.Update(task);
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
                             && task.Status != ProjectTasksStatus.Completed
                             && task.Status != ProjectTasksStatus.Cancelled;

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
        public async Task<Result<PagedResult<TaskDto>>> FilterAsync(
    TaskFilterDto filter, int? actingManagerCompanyId)
        {
            var pageNumber = filter.PageNumber < 1 ? 1 : filter.PageNumber;
            var pageSize = filter.PageSize is < 1 or > 100 ? 10 : filter.PageSize;

            var (items, totalCount) = await _unitOfWork.Tasks.FilterAsync(
                filter.AssignedToUserId,
                filter.Priority,
                filter.Status,
                filter.ProjectId,
                filter.CompanyId,
                actingManagerCompanyId,
                pageNumber,
                pageSize);

            var dtos = new List<TaskDto>();
            foreach (var task in items)
            {
                dtos.Add(await MapToDtoAsync(task));
            }

            return Result<PagedResult<TaskDto>>.Success(new PagedResult<TaskDto>(dtos, pageNumber, pageSize, totalCount));
        }
    }
}
