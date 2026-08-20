using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using TaskTrackerBLL.Common;
using TaskTrackerBLL.DTOs.Project;
using TaskTrackerBLL.DTOs.Tasks;
using TaskTrackerBLL.Interfaces;
using TaskTrackerBLL.Interfaces.Services;

using TaskTrackerDAL.Models;
using TaskTrackerDAL.Models.Enums;

namespace TaskTrackerBLL.Services
{
    public class TaskService : ITaskService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;

        public TaskService(
            IUnitOfWork unitOfWork,
            IAuditService auditService)
        {
            _unitOfWork = unitOfWork;
            _auditService = auditService;
        }

        // ============================================================
        // GET BY ID
        // ============================================================

        public async Task<Result<TaskDto>> GetByIdAsync(int id)
        {
            var task = await _unitOfWork.Tasks.GetByIdAsync(id);

            if (task is null)
            {
                return Result<TaskDto>.Failure(
                    $"Task with ID {id} was not found.");
            }

            return Result<TaskDto>.Success(
                await MapToDtoAsync(task));
        }

        // ============================================================
        // GET TASKS BY PROJECT
        // ============================================================

        public async Task<Result<IReadOnlyList<TaskDto>>> GetByProjectIdAsync(
            int projectId)
        {
            var tasks = await _unitOfWork.Tasks
                .GetByProjectIdAsync(projectId);

            var dtos = new List<TaskDto>();

            foreach (var task in tasks)
            {
                dtos.Add(await MapToDtoAsync(task));
            }

            return Result<IReadOnlyList<TaskDto>>.Success(dtos);
        }

        // ============================================================
        // GET TASKS BY USER
        // ============================================================

        public async Task<Result<IReadOnlyList<TaskDto>>> GetByAssignedUserIdAsync(
            int userId)
        {
            var tasks = await _unitOfWork.Tasks
                .GetByAssignedUserIdAsync(userId);

            var dtos = new List<TaskDto>();

            foreach (var task in tasks)
            {
                dtos.Add(await MapToDtoAsync(task));
            }

            return Result<IReadOnlyList<TaskDto>>.Success(dtos);
        }

        // ============================================================
        // GET OVERDUE TASKS
        // ============================================================

        public async Task<Result<IReadOnlyList<TaskDto>>> GetOverdueTasksAsync(
            int companyId)
        {
            var tasks = await _unitOfWork.Tasks
                .GetOverdueTasksAsync(companyId);

            var dtos = new List<TaskDto>();

            foreach (var task in tasks)
            {
                dtos.Add(await MapToDtoAsync(task));
            }

            return Result<IReadOnlyList<TaskDto>>.Success(dtos);
        }

        // ============================================================
        // CREATE TASK
        // ============================================================

        public async Task<Result<TaskDto>> CreateAsync(
            CreateTaskDto dto,
            int createdByUserId,
            int? actingManagerCompanyId)
        {
            // --------------------------------------------------------
            // 1. Check project
            // --------------------------------------------------------

            var project = await _unitOfWork.Projects
                .GetByIdAsync(dto.ProjectId);

            if (project is null)
            {
                return Result<TaskDto>.Failure(
                    $"Project with ID {dto.ProjectId} was not found.");
            }

            // --------------------------------------------------------
            // 2. Manager company scope validation
            // --------------------------------------------------------

            if (actingManagerCompanyId.HasValue &&
                project.CompanyId != actingManagerCompanyId.Value)
            {
                return Result<TaskDto>.Failure(
                    "You are not authorized to create tasks for this project.");
            }

            // --------------------------------------------------------
            // 3. Validate selected users
            // --------------------------------------------------------

            var selectedUserIds = dto.AssignedToUserIds
                .Distinct()
                .ToList();

            foreach (var userId in selectedUserIds)
            {
                var isProjectMember =
                    await _unitOfWork.Projects
                        .IsUserProjectMemberAsync(
                            dto.ProjectId,
                            userId);

                if (!isProjectMember)
                {
                    return Result<TaskDto>.Failure(
                        $"User with ID {userId} is not a member of this project.");
                }
            }

            // --------------------------------------------------------
            // 4. Create ProjectTask
            // --------------------------------------------------------

            var task = new ProjectTask
            {
                ProjectId = dto.ProjectId,
                Title = dto.Title,
                Description = dto.Description,

                Status = dto.Status,

                Priority = dto.Priority,
                DueDate = dto.DueDate,

                CreatedByUserId = createdByUserId
            };

            await _unitOfWork.Tasks.AddAsync(task);

            // First save because TaskId is generated by database.
            await _unitOfWork.SaveChangesAsync();

            // --------------------------------------------------------
            // 5. Add TaskMembers
            // --------------------------------------------------------

            foreach (var userId in selectedUserIds)
            {
                var taskMember = new TaskMember
                {
                    TaskId = task.Id,
                    UserId = userId
                };

                await _unitOfWork.TaskMembers
                    .AddAsync(taskMember);
            }

            // --------------------------------------------------------
            // 6. Save TaskMembers
            // --------------------------------------------------------

            await _unitOfWork.SaveChangesAsync();

            // --------------------------------------------------------
            // 7. Return DTO
            // --------------------------------------------------------

            return Result<TaskDto>.Success(
                await MapToDtoAsync(task));
        }

        // ============================================================
        // DELETE TASK
        // ============================================================

        public async Task<Result> DeleteAsync(
            int id,
            int actingUserId,
            int? actingManagerCompanyId)
        {
            var task = await _unitOfWork.Tasks.GetByIdAsync(id);

            if (task is null)
            {
                return Result.Failure(
                    $"Task with ID {id} was not found.");
            }

            // Manager scope validation
            var scopeResult =
                await ValidateManagerScopeAsync(
                    task,
                    actingManagerCompanyId);

            if (!scopeResult.Succeeded)
            {
                return scopeResult;
            }

            var snapshot =
                $"{task.Title} (Project #{task.ProjectId})";

            // Delete task
            _unitOfWork.Tasks.Remove(task);

            await _unitOfWork.SaveChangesAsync();

            // Audit
            await _auditService.LogDeletedAsync(
                "ProjectTask",
                id,
                actingUserId,
                snapshot);

            return Result.Success();
        }

        // ============================================================
        // ASSIGN TASK
        // ============================================================

        public async Task<Result> AssignAsync(
            AssignTaskDto dto,
            int? actingManagerCompanyId)
        {
            var task = await _unitOfWork.Tasks
                .GetByIdAsync(dto.TaskId);

            if (task is null)
            {
                return Result.Failure(
                    $"Task with ID {dto.TaskId} was not found.");
            }

            // Manager scope
            var scopeResult =
                await ValidateManagerScopeAsync(
                    task,
                    actingManagerCompanyId);

            if (!scopeResult.Succeeded)
            {
                return scopeResult;
            }

            // Check project membership
            var isProjectMember =
                await _unitOfWork.Projects
                    .IsUserProjectMemberAsync(
                        task.ProjectId,
                        dto.AssignedToUserId);

            if (!isProjectMember)
            {
                return Result.Failure(
                    "The user must be a member of this project.");
            }

            // Check whether already assigned
            var alreadyAssigned =
                await _unitOfWork.TaskMembers
                    .ExistsAsync(
                        dto.TaskId,
                        dto.AssignedToUserId);

            if (alreadyAssigned)
            {
                return Result.Failure(
                    "This user is already assigned to this task.");
            }

            // Add member
            var taskMember = new TaskMember
            {
                TaskId = dto.TaskId,
                UserId = dto.AssignedToUserId
            };

            await _unitOfWork.TaskMembers
                .AddAsync(taskMember);

            await _unitOfWork.SaveChangesAsync();

            return Result.Success();
        }

        // ============================================================
        // REASSIGN TASK
        // ============================================================

        public async Task<Result> ReassignAsync(
            AssignTaskDto dto,
            int? actingManagerCompanyId)
        {
            var task = await _unitOfWork.Tasks
                .GetByIdAsync(dto.TaskId);

            if (task is null)
            {
                return Result.Failure(
                    $"Task with ID {dto.TaskId} was not found.");
            }

            // Manager scope
            var scopeResult =
                await ValidateManagerScopeAsync(
                    task,
                    actingManagerCompanyId);

            if (!scopeResult.Succeeded)
            {
                return scopeResult;
            }

            // Check new user project membership
            var isProjectMember =
                await _unitOfWork.Projects
                    .IsUserProjectMemberAsync(
                        task.ProjectId,
                        dto.AssignedToUserId);

            if (!isProjectMember)
            {
                return Result.Failure(
                    "The new user must be a member of this project.");
            }

            // Get current task members
            var currentMembers =
                await _unitOfWork.TaskMembers
                    .GetByTaskIdAsync(dto.TaskId);

            if (currentMembers.Count == 0)
            {
                return Result.Failure(
                    "This task has no current members. Use Assign instead.");
            }

            // Already assigned to this user?
            if (currentMembers.Any(
                x => x.UserId == dto.AssignedToUserId))
            {
                return Result.Failure(
                    "This user is already assigned to this task.");
            }

            // --------------------------------------------------------
            // Reassign means:
            // remove existing members
            // add new member
            // --------------------------------------------------------

            foreach (var member in currentMembers)
            {
                await _unitOfWork.TaskMembers
                    .RemoveAsync(member);
            }

            var newMember = new TaskMember
            {
                TaskId = dto.TaskId,
                UserId = dto.AssignedToUserId
            };

            await _unitOfWork.TaskMembers
                .AddAsync(newMember);

            await _unitOfWork.SaveChangesAsync();

            return Result.Success();
        }

        // ============================================================
        // CHANGE DEADLINE
        // ============================================================

        public async Task<Result> ChangeDeadlineAsync(
            ChangeDeadlineDto dto,
            int? actingManagerCompanyId)
        {
            var task = await _unitOfWork.Tasks
                .GetByIdAsync(dto.TaskId);

            if (task is null)
            {
                return Result.Failure(
                    $"Task with ID {dto.TaskId} was not found.");
            }

            var scopeResult =
                await ValidateManagerScopeAsync(
                    task,
                    actingManagerCompanyId);

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

        // ============================================================
        // CHANGE PRIORITY
        // ============================================================

        public async Task<Result> ChangePriorityAsync(
            ChangePriorityDto dto,
            int? actingManagerCompanyId)
        {
            var task = await _unitOfWork.Tasks
                .GetByIdAsync(dto.TaskId);

            if (task is null)
            {
                return Result.Failure(
                    $"Task with ID {dto.TaskId} was not found.");
            }

            var scopeResult =
                await ValidateManagerScopeAsync(
                    task,
                    actingManagerCompanyId);

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

        // ============================================================
        // CHANGE STATUS
        // ============================================================

        public async Task<Result> ChangeStatusAsync(
            MoveTaskStatusDto dto,
            int actingUserId,
            bool isManager)
        {
            var task = await _unitOfWork.Tasks
                .GetByIdAsync(dto.TaskId);

            if (task is null)
            {
                return Result.Failure(
                    $"Task with ID {dto.TaskId} was not found.");
            }

            // --------------------------------------------------------
            // Employee validation
            // --------------------------------------------------------

            if (!isManager)
            {
                // Completed / Cancelled cannot be changed
                if (task.Status == ProjectTasksStatus.Completed ||
                    task.Status == ProjectTasksStatus.Cancelled)
                {
                    return Result.Failure(
                        $"This task is already {task.Status} " +
                        "and its status can no longer be changed.");
                }

                // User must be TaskMember
                var isTaskMember =
                    await _unitOfWork.TaskMembers
                        .ExistsAsync(
                            dto.TaskId,
                            actingUserId);

                if (!isTaskMember)
                {
                    return Result.Failure(
                        "You can only update the status of tasks assigned to you.");
                }
            }

            // --------------------------------------------------------
            // Update status
            // --------------------------------------------------------

            task.Status = dto.NewStatus;
            task.UpdatedAt = DateTime.UtcNow;

            // Completion date
            if (dto.NewStatus == ProjectTasksStatus.Completed)
            {
                task.CompletedAt = DateTime.UtcNow;
            }
            else
            {
                task.CompletedAt = null;
            }

            _unitOfWork.Tasks.Update(task);

            await _unitOfWork.SaveChangesAsync();

            return Result.Success();
        }

        // ============================================================
        // GET PROGRESS NOTES
        // ============================================================

        public async Task<Result<IReadOnlyList<TaskProgressNoteDto>>>
            GetProgressNotesAsync(int taskId)
        {
            var notes = await _unitOfWork.Tasks
                .GetProgressNotesAsync(taskId);

            var dtos = notes
                .Select(n => new TaskProgressNoteDto
                {
                    Id = n.Id,
                    AuthorName = n.AuthorUser.FullName,
                    Note = n.Note,
                    IsCompletionComment = n.IsCompletionComment,
                    CreatedAt = n.CreatedAt
                })
                .ToList();

            return Result<IReadOnlyList<TaskProgressNoteDto>>
                .Success(dtos);
        }

        // ============================================================
        // ADD PROGRESS NOTE
        // ============================================================

        public async Task<Result> AddProgressNoteAsync(
            AddProgressNoteDto dto,
            int authorUserId)
        {
            var task = await _unitOfWork.Tasks
                .GetByIdAsync(dto.TaskId);

            if (task is null)
            {
                return Result.Failure(
                    $"Task with ID {dto.TaskId} was not found.");
            }

            // User must be TaskMember
            var isTaskMember =
                await _unitOfWork.TaskMembers
                    .ExistsAsync(
                        dto.TaskId,
                        authorUserId);

            if (!isTaskMember)
            {
                return Result.Failure(
                    "You can only add notes to tasks assigned to you.");
            }

            var note = new TaskProgressNote
            {
                TaskId = dto.TaskId,
                AuthorUserId = authorUserId,
                Note = dto.Note,
                IsCompletionComment = false
            };

            await _unitOfWork.Tasks
                .AddProgressNoteAsync(note);

            await _unitOfWork.SaveChangesAsync();

            return Result.Success();
        }

        // ============================================================
        // ADD COMPLETION COMMENT
        // ============================================================

        public async Task<Result> AddCompletionCommentAsync(
            AddCompletionCommentDto dto,
            int authorUserId)
        {
            var task = await _unitOfWork.Tasks
                .GetByIdAsync(dto.TaskId);

            if (task is null)
            {
                return Result.Failure(
                    $"Task with ID {dto.TaskId} was not found.");
            }

            // User must be TaskMember
            var isTaskMember =
                await _unitOfWork.TaskMembers
                    .ExistsAsync(
                        dto.TaskId,
                        authorUserId);

            if (!isTaskMember)
            {
                return Result.Failure(
                    "You can only comment on tasks assigned to you.");
            }

            // Task must be completed
            if (task.Status != ProjectTasksStatus.Completed)
            {
                return Result.Failure(
                    "A completion comment can only be added " +
                    "once the task is marked Completed.");
            }

            var note = new TaskProgressNote
            {
                TaskId = dto.TaskId,
                AuthorUserId = authorUserId,
                Note = dto.Comment,
                IsCompletionComment = true
            };

            await _unitOfWork.Tasks
                .AddProgressNoteAsync(note);

            await _unitOfWork.SaveChangesAsync();

            return Result.Success();
        }

        // ============================================================
        // MANAGER SCOPE VALIDATION
        // ============================================================

        private async Task<Result> ValidateManagerScopeAsync(
            ProjectTask task,
            int? actingManagerCompanyId)
        {
            // No company restriction
            if (!actingManagerCompanyId.HasValue)
            {
                return Result.Success();
            }

            var project = await _unitOfWork.Projects
                .GetByIdAsync(task.ProjectId);

            if (project is null)
            {
                return Result.Failure(
                    "The project associated with this task was not found.");
            }

            if (project.CompanyId != actingManagerCompanyId.Value)
            {
                return Result.Failure(
                    "You are not authorized to manage this task.");
            }

            return Result.Success();
        }
        
        
        // ============================================================
        // MAP ENTITY -> DTO
        // ============================================================

        private async Task<TaskDto> MapToDtoAsync(
            ProjectTask task)
        {
            // --------------------------------------------------------
            // Project
            // --------------------------------------------------------

            var project = await _unitOfWork.Projects
                .GetByIdAsync(task.ProjectId);

            // --------------------------------------------------------
            // Task Members
            // --------------------------------------------------------

            var taskMembers =
                await _unitOfWork.TaskMembers
                    .GetByTaskIdAsync(task.Id);

            // --------------------------------------------------------
            // Existing TaskDto is still using singular
            // AssignedToUserId.
            //
            // Since TaskMember supports multiple users, we use
            // the first member here for backward compatibility.
            // --------------------------------------------------------

            var assignedToUserIds = taskMembers
    .Select(x => x.UserId)
    .ToList();

            var assignedToUserNames = taskMembers
                .Where(x => x.User != null)
                .Select(x => x.User!.FullName)
                .ToList();

            // --------------------------------------------------------
            // Overdue
            // --------------------------------------------------------

            var isOverdue =
                task.DueDate.HasValue &&
                task.DueDate.Value.Date < DateTime.UtcNow.Date &&
                task.Status != ProjectTasksStatus.Completed &&
                task.Status != ProjectTasksStatus.Cancelled;

            // --------------------------------------------------------
            // DTO
            // --------------------------------------------------------

            return new TaskDto
            {
                Id = task.Id,

                ProjectId = task.ProjectId,

                ProjectName =
                    project?.Name ?? "Unknown",

                Title = task.Title,

                Description = task.Description,

                AssignedToUserIds =
    assignedToUserIds,

                AssignedToUserNames =
    assignedToUserNames,

                Status = task.Status,

                Priority = task.Priority,

                DueDate = task.DueDate,

                IsOverdue = isOverdue,

                CompletedAt = task.CompletedAt,

                CreatedAt = task.CreatedAt
            };
        }

        // ============================================================
        // FILTER
        // ============================================================

        public async Task<Result<PagedResult<TaskDto>>> FilterAsync(
            TaskFilterDto filter,
            int? actingManagerCompanyId)
        {
            var pageNumber =
                filter.PageNumber < 1
                    ? 1
                    : filter.PageNumber;

            var pageSize =
                filter.PageSize is < 1 or > 100
                    ? 10
                    : filter.PageSize;

            var (items, totalCount) =
                await _unitOfWork.Tasks.FilterAsync(
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
                dtos.Add(
                    await MapToDtoAsync(task));
            }

            var result =
                new PagedResult<TaskDto>(
                    dtos,
                    pageNumber,
                    pageSize,
                    totalCount);

            return Result<PagedResult<TaskDto>>
                .Success(result);
        }
        public async Task<Result> AssignMembersAsync(
    int taskId,
    List<int> assignedToUserIds,
    int actingUserId)
        {
            var task = await _unitOfWork.Tasks.GetByIdAsync(taskId);

            if (task == null)
            {
                return Result.Failure("Task not found.");
            }


            assignedToUserIds ??= new List<int>();


            assignedToUserIds = assignedToUserIds
                .Distinct()
                .ToList();


            // =====================================================
            // REMOVE EXISTING MEMBERS
            // =====================================================

            var existingMembers = task.TaskMembers?.ToList()
                                   ?? new List<TaskMember>();


            foreach (var member in existingMembers)
            {
                task.TaskMembers.Remove(member);
            }


            // =====================================================
            // ADD NEW MEMBERS
            // =====================================================

            foreach (var userId in assignedToUserIds)
            {
                task.TaskMembers.Add(new TaskMember
                {
                    TaskId = taskId,
                    UserId = userId
                });
            }


            // =====================================================
            // UPDATE
            // =====================================================

            task.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Tasks.Update(task);

            await _unitOfWork.SaveChangesAsync();


            return Result.Success();
        }
    }
}