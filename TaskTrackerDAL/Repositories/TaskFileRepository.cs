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
    public class TaskFileRepository : GenericRepository<TaskFile>, ITaskFileFeature
    {
        public TaskFileRepository(TaskTrackerDbContext context)
            : base(context)
        {
        }

        public async Task<IReadOnlyList<TaskFile>> GetByTaskIdAsync(int taskId)
        {
            return await _context.TaskFiles
                .AsNoTracking()
                .Where(f => f.TaskId == taskId)
                .OrderByDescending(f => f.UploadedAt)
                .ToListAsync();
        }

        public async Task<TaskFile?> GetByIdWithTaskAsync(int id)
        {
            return await _context.TaskFiles
                .Include(f => f.Task)
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.Id == id);
        }

        public async Task DeleteByTaskIdAsync(int taskId)
        {
            var files = await _context.TaskFiles
                .Where(f => f.TaskId == taskId)
                .ToListAsync();

            if (files.Count > 0)
            {
                _context.TaskFiles.RemoveRange(files);
            }
        }
    }
}
