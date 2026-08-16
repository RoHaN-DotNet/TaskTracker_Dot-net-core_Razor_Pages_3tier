using System;
using System.Collections.Generic;
using System.Text;
using TaskTrackerDAL.Interfaces.Generic;
using TaskTrackerDAL.Models;

namespace TaskTrackerDAL.Interfaces
{
    public interface ITaskFileFeature: IGenericFeature<TaskFile>
    {
        Task<IReadOnlyList<TaskFile>> GetByTaskIdAsync(int taskId);

        Task<TaskFile?> GetByIdWithTaskAsync(int id);

        Task DeleteByTaskIdAsync(int taskId);
    }
}
