using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using TaskTrackerDAL.Data;
using TaskTrackerDAL.Interfaces;
using TaskTrackerDAL.Models;
using TaskTrackerDAL.Repositories.Generic;

namespace TaskTrackerDAL.Repositories
{
    public class TaskTransferHistoryRepository: GenericRepository<TaskTransferHistory>, ITaskTransferHistoryFeature
    {
        public TaskTransferHistoryRepository(TaskTrackerDbContext context)
            : base(context)
        {
        }

        public async Task<IReadOnlyList<TaskTransferHistory>> GetByTaskIdAsync(int taskId)
        {
            return await _context.TaskTransferHistories
                .Include(h => h.FromUser)
                .Include(h => h.ToUser)
                .Include(h => h.TransferredByUser)
                .AsNoTracking()
                .Where(h => h.TaskId == taskId)
                .OrderByDescending(h => h.CreatedAt)
                .ToListAsync();
        }
    }
}
