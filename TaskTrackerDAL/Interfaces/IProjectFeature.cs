using System;
using System.Collections.Generic;
using System.Text;
using TaskTrackerDAL.Interfaces.Generic;
using TaskTrackerDAL.Models;
using TaskTrackerDAL.Models.Enums;

namespace TaskTrackerDAL.Interfaces
{
    public interface IProjectFeature:IGenericFeature<Project>
    {
        Task<Project?> GetByIdWithMembersAsync(int projectId);

        Task<Project?> GetByIdWithTasksAsync(int projectId);

        Task<IReadOnlyList<Project>> GetByCompanyIdAsync(int companyId);

        Task<IReadOnlyList<Project>> GetByMemberUserIdAsync(int userId);

        Task<int> CountByStatusAsync(int companyId, ProjectStatus status);

        Task<bool> IsUserProjectMemberAsync(int projectId, int userId);

        Task<(IReadOnlyList<Project> Items, int TotalCount)> SearchByCompanyAsync(
            int? companyId,
            string? searchTerm,
            ProjectStatus? status,
            int pageNumber,
            int pageSize);
        Task<(IReadOnlyList<Project> Items, int TotalCount)> FilterAsync(
            int? companyId,
            ProjectStatus? status,
            TaskPriority? priority,
            DateTime? deadlineFreom,
            DateTime? deadlineTo,
            int? enforcedCompanyId,
            int pageNumber,
            int pageSize

            );

    }
}
