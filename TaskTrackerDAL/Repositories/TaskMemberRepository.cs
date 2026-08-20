using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using TaskTrackerDAL.Data;
using TaskTrackerDAL.Interfaces;
using TaskTrackerDAL.Interfaces.Generic;
using TaskTrackerDAL.Models;
using TaskTrackerDAL.Repositories.Generic;

namespace TaskTrackerDAL.Repositories
{
    public class TaskMemberRepository: GenericRepository<TaskMember>,ITaskMemberFeature
    {
        
        public TaskMemberRepository(TaskTrackerDbContext context):base(context)
        {
            
        }

        

        public async Task RemoveAsync(TaskMember taskMember)
        {
            _context.TaskMembers.Remove(taskMember);

            await Task.CompletedTask;
        }

        public async Task<List<TaskMember>> GetByTaskIdAsync(int taskId)
        {
            return await _context.TaskMembers
                .Include(tm => tm.User)
                .Where(tm => tm.TaskId == taskId)
                .ToListAsync();
        }

        public async Task<bool> ExistsAsync(int taskId, int userId)
        {
            return await _context.TaskMembers
                .AnyAsync(tm =>
                    tm.TaskId == taskId &&
                    tm.UserId == userId);
        }
        public async Task<List<TaskMember>> GetByUserIdAsync(int userId)
        {
            return await _dbSet
                .Include(tm => tm.Task)
                .Where(tm => tm.UserId == userId)
                .ToListAsync();
        }
    }
}

