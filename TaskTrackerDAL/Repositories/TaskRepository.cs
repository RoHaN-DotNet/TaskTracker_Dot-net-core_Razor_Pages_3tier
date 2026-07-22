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
    public class TaskRepository : GenericRepository<ProjectTask>, ITaskFeature
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
        public async Task<IReadOnlyList<TaskProgressNote>> GetProgressNotesAsync(int taskId)
        {
            return await _context.Set<TaskProgressNote>()
                .Include(n => n.AuthorUser)
                .AsNoTracking()
                .Where(n => n.TaskId == taskId)
                .OrderBy(n => n.CreatedAt)
                .ToListAsync();
        }

        public async Task AddProgressNoteAsync(TaskProgressNote note)
        {
            await _context.Set<TaskProgressNote>().AddAsync(note);
        }
        public async Task<int> CountOverdueAsync(int? companyId)
        {
            var today = DateTime.UtcNow.Date;
            var query = _context.Tasks.AsNoTracking().Where(t =>
            t.DueDate != null &&
            t.DueDate < today &&
            t.Status != ProjectTasksStatus.Completed &&
            t.Status != ProjectTasksStatus.Completed
            );
            if (companyId.HasValue)
            {
                query=query.Where(t=>t.Project.CompanyId== companyId.Value);
            }
            return await query.CountAsync();
        }
        public async Task<IReadOnlyList<ProjectTask>> GetDueTodayAsync(int? companyId, int? userId)
        {
            var today= DateTime.UtcNow.Date;

            var query = _context.Tasks.AsNoTracking().Where(t =>
            t.DueDate != null &&
            t.DueDate.Value.Date == today &&
            t.Status != ProjectTasksStatus.Completed &&
            t.Status != ProjectTasksStatus.Cancelled
            );

            if (companyId.HasValue)
            {
                query=query.Where(t=> t.Project.CompanyId== companyId.Value);
            }
            if (userId.HasValue)
            {
                query=query.Where(t=>t.AssignedToUserId== userId.Value);
            }
            return await query.OrderBy(t => t.Priority).ToListAsync();
        }
        public async Task<IReadOnlyList<ProjectTask>> GetRecentAsync(int companyId, int count)
        {
            return await _context.Tasks
                .AsNoTracking()
                .Where(t => t.Project.CompanyId == companyId)
                .OrderByDescending(t => t.CreatedAt)
                .Take(count)
                .ToListAsync();
        }
        public async Task<(IReadOnlyList<ProjectTask> Items, int TotalCount)> FilterAsync(
    int? assignedToUserId,
    TaskPriority? priority,
    ProjectTasksStatus? status,
    int? projectId,
    int? companyId,
    int? enforcedCompanyId,
    int pageNumber,
    int pageSize)
        {
            var query = _context.Tasks.AsNoTracking().AsQueryable();

            if (enforcedCompanyId.HasValue)
            {
                query = query.Where(t => t.Project.CompanyId == enforcedCompanyId.Value);
            }

            if (companyId.HasValue)
            {
                query = query.Where(t => t.Project.CompanyId == companyId.Value);
            }

            if (projectId.HasValue)
            {
                query = query.Where(t => t.ProjectId == projectId.Value);
            }

            if (assignedToUserId.HasValue)
            {
                query = query.Where(t => t.AssignedToUserId == assignedToUserId.Value);
            }

            if (priority.HasValue)
            {
                query = query.Where(t => t.Priority == priority.Value);
            }

            if (status.HasValue)
            {
                query = query.Where(t => t.Status == status.Value);
            }

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderBy(t => t.DueDate ?? DateTime.MaxValue)
                .ThenByDescending(t => t.Priority)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }
        public async Task<IReadOnlyList<(int UserId, int TotalAssigned, int Completed)>> GetEmployeePerformanceAsync(int companyId)
        {
            var grouped = await _context.Tasks
                .AsNoTracking()
                .Where(t => t.Project.CompanyId == companyId && t.AssignedToUserId != null)
                .GroupBy(t => t.AssignedToUserId!.Value)
                .Select(g => new
                {
                    UserId = g.Key,
                    TotalAssigned = g.Count(),
                    Completed = g.Count(t => t.Status == ProjectTasksStatus.Completed)
                })
                .ToListAsync();

            return grouped.Select(x => (x.UserId, x.TotalAssigned, x.Completed)).ToList();
        }
    }
}

