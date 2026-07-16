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
    }
}
