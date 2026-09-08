using System;
using System.Collections.Generic;
using System.Text;
using TaskTrackerBLL.Common;
using TaskTrackerBLL.DTOs.Project;
using TaskTrackerBLL.DTOs.Tasks;

namespace TaskTrackerBLL.Interfaces.Services
{
    public interface ITaskService
    {
        Task<Result<TaskDto>> GetByIdAsync(int id);

        Task<Result<IReadOnlyList<TaskDto>>> GetByProjectIdAsync(int projectId);

        Task<Result<IReadOnlyList<TaskDto>>> GetByAssignedUserIdAsync(int userId);

        Task<Result<IReadOnlyList<TaskDto>>> GetOverdueTasksAsync(int companyId);

        Task<Result<TaskDto>> CreateAsync(
            CreateTaskDto dto, int createdByUserId, int? actingManagerCompanyId);

        Task<Result> DeleteAsync(int id, int actingUserId, int? actingManagerCompanyId);
        Task<Result> AssignAsync(AssignTaskDto dto, int? actingManagerCompanyId);

        Task<Result> ReassignAsync(AssignTaskDto dto, int? actingManagerCompanyId);

        Task<Result> ChangeDeadlineAsync(ChangeDeadlineDto dto, int? actingManagerCompanyId);

        Task<Result> ChangePriorityAsync(ChangePriorityDto dto, int? actingManagerCompanyId);

        Task<Result> ChangeStatusAsync(MoveTaskStatusDto dto, int actingUserId, bool isManager);

        // NEW: updates Title/Description — nothing previously did this.
        Task<Result> UpdateDetailsAsync(int taskId, string title, string? description, int? actingManagerCompanyId);
        // NEW: transfer task from one member to another, with a note + history log
        Task<Result> TransferAsync(TransferTaskDto dto, int actingUserId, int? actingManagerCompanyId);

        Task<Result<IReadOnlyList<TaskTransferHistoryDto>>> GetTransferHistoryAsync(int taskId);
        Task<Result<IReadOnlyList<TaskProgressNoteDto>>> GetProgressNotesAsync(int taskId);

        Task<Result> AddProgressNoteAsync(AddProgressNoteDto dto, int authorUserId);

        Task<Result> AddCompletionCommentAsync(AddCompletionCommentDto dto, int authorUserId);

        Task<Result<PagedResult<TaskDto>>> FilterAsync(TaskFilterDto filter, int? actingManagerCompanyId);
        Task<Result> AssignMembersAsync(
    int taskId,
    List<int> assignedToUserIds,
    int actingUserId);
    }
}