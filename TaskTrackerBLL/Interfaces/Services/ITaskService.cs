using System;
using System.Collections.Generic;
using System.Text;
using TaskTrackerBLL.Common;
using TaskTrackerBLL.DTOs.Task;

namespace TaskTrackerBLL.Interfaces.Services
{
    public interface ITaskService
    {
        Task<Result<TaskDto>> GetByIdAsync(int id);

        Task<Result<IReadOnlyList<TaskDto>>> GetByProjectIdAsync(int projectId);

        Task<Result<IReadOnlyList<TaskDto>>> GetByAssignedUserIdAsync(int userId);

        Task<Result<IReadOnlyList<TaskDto>>> GetOverdueTasksAsync(int companyId);

        Task<Result<TaskDto>> CreateAsync(CreateTaskDto dto, int createdByUserId);

        Task<Result> UpdateAsync(UpdateTaskDto dto);

        Task<Result> DeleteAsync(int id);
    }
}
