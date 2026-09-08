using System;
using System.Collections.Generic;
using System.Text;
using TaskTrackerDAL.Interfaces.Generic;
using TaskTrackerDAL.Models;

namespace TaskTrackerDAL.Interfaces
{
    public interface ITaskTransferHistoryFeature : IGenericFeature<TaskTransferHistory>
    {
        Task<IReadOnlyList<TaskTransferHistory>> GetByTaskIdAsync(int taskId);
    }
}
