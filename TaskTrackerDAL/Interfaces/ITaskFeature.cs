using System;
using System.Collections.Generic;
using System.Text;
using TaskTrackerDAL.Interfaces.Generic;
using TaskTrackerDAL.Models;
using TaskTrackerDAL.Models.Enums;
using TaskTrackerDAL.Repositories.Generic;

namespace TaskTrackerDAL.Interfaces
{
    public interface ITaskFeature : IGenericFeature<ProjectTask>
    {
        Task<IReadOnlyList<ProjectTask>> GetByProjectIdAsync(int projectId);

        Task<IReadOnlyList<ProjectTask>> GetByAssignedUserIdAsync(int userId);

        Task<IReadOnlyList<ProjectTask>> GetOverdueTasksAsync(int companyId);

        Task<int> CountByStatusAsync(int companyId, ProjectTasksStatus status);

        Task<Dictionary<int, int>> GetOpenTaskCountsByUserAsync(int companyId);
        Task<IReadOnlyList<TaskProgressNote>> GetProgressNotesAsync(int taskId);

        Task AddProgressNoteAsync(TaskProgressNote note);

        Task<int> CountOverdueAsync(int? companyId);
        Task<IReadOnlyList<ProjectTask>> GetDueTodayAsync(int? companyId, int? userId);
        Task<IReadOnlyList<ProjectTask>> GetRecentAsync(int companyId, int count);
        Task<Dictionary<int, int>> GetOverdueTaskCountsByUserAsync(
    int companyId);
        Task<(IReadOnlyList<ProjectTask> Items, int TotalCount)> FilterAsync(
      int? assignedToUserId,
      TaskPriority? priority,
      ProjectTasksStatus? status,
      int? projectId,
      int? companyId,
      int? enforcedCompanyId,
      int pageNumber,
      int pageSize);

        Task<IReadOnlyList<(int UserId, int TotalAssigned, int Completed)>> GetEmployeePerformanceAsync(int companyId);
    }
}
