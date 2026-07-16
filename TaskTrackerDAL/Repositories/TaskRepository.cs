using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using TaskTrackerDAL.Data;
using TaskTrackerDAL.Interfaces;
using TaskTrackerDAL.Models;
using TaskTrackerDAL.Models.Enums;
using TaskTrackerDAL.Repositories.Generic;
using static TaskTrackerDAL.Repositories.TaskRepository;

namespace TaskTrackerDAL.Repositories
{
    public class TaskRepository:GenericRepository<ProjectTask>,ITaskFeature
    {
        
            public TaskRepository(TaskTrackerDbContext context) : base(context)
            {
            }

            public async Task<IReadOnlyList<ProjectTask>> GetByProjectIdAsync(int projectId)
            {
                return await _context.Tasks
                    .AsNoTracking()
                    .Where(t => t.ProjectId == projectId)
                    .OrderBy(t => t.Priority)
                    .ThenBy(t => t.DueDate)
                    .ToListAsync();
            }

            public async Task<IReadOnlyList<ProjectTask>> GetByAssignedUserIdAsync(int userId)
            {
                return await _context.Tasks
                    .AsNoTracking()
                    .Where(t => t.AssignedToUserId == userId)
                    .OrderBy(t => t.DueDate)
                    .ToListAsync();
            }

            public async Task<IReadOnlyList<ProjectTask>> GetOverdueTasksAsync(int companyId)
            {
                var today = DateTime.UtcNow.Date;

                return await _context.Tasks
                    .AsNoTracking()
                    .Where(t => t.Project.CompanyId == companyId
                             && t.DueDate != null
                             && t.DueDate < today
                             && t.Status != ProjectTasksStatus.Completed)
                    .OrderBy(t => t.DueDate)
                    .ToListAsync();
            }

            public async Task<int> CountByStatusAsync(int companyId, ProjectTasksStatus status)
            {
                return await _context.Tasks
                    .AsNoTracking()
                    .CountAsync(t => t.Project.CompanyId == companyId && t.Status == status);
            }
        
            //How many tasks are assigned to same member
            public async Task<Dictionary<int, int>> GetOpenTaskCountsByUserAsync(int companyId)
            {
                return await _context.Tasks
                    .AsNoTracking()
                    .Where(t => t.Project.CompanyId == companyId
                             && t.AssignedToUserId != null
                             && t.Status != ProjectTasksStatus.Completed)
                    .GroupBy(t => t.AssignedToUserId!.Value)
                    .Select(g => new { UserId = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.UserId, x => x.Count);
            }
        }
}
