using Microsoft.EntityFrameworkCore;
using TaskTrackerDAL.Data;
using TaskTrackerDAL.Interfaces;
using TaskTrackerDAL.Models;
using TaskTrackerDAL.Models.Enums;
using TaskTrackerDAL.Repositories.Generic;

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

        // Get tasks assigned to a specific user through TaskMembers
        public async Task<IReadOnlyList<ProjectTask>> GetByAssignedUserIdAsync(int userId)
        {
            return await _context.Tasks
                .AsNoTracking()
                .Where(t => t.TaskMembers.Any(tm => tm.UserId == userId))
                .OrderBy(t => t.DueDate)
                .ToListAsync();
        }

        public async Task<IReadOnlyList<ProjectTask>> GetOverdueTasksAsync(int companyId)
        {
            var today = DateTime.UtcNow.Date;

            return await _context.Tasks
                .AsNoTracking()
                .Where(t =>
                    t.Project.CompanyId == companyId &&
                    t.DueDate != null &&
                    t.DueDate < today &&
                    t.Status != ProjectTasksStatus.Completed &&
                    t.Status != ProjectTasksStatus.Cancelled)
                .OrderBy(t => t.DueDate)
                .ToListAsync();
        }

        public async Task<int> CountByStatusAsync(
            int companyId,
            ProjectTasksStatus status)
        {
            return await _context.Tasks
                .AsNoTracking()
                .CountAsync(t =>
                    t.Project.CompanyId == companyId &&
                    t.Status == status);
        }

        // How many open tasks are assigned to each member
        public async Task<Dictionary<int, int>> GetOpenTaskCountsByUserAsync(
            int companyId)
        {
            return await _context.TaskMembers
                .AsNoTracking()
                .Where(tm =>
                    tm.Task.Project.CompanyId == companyId &&
                    tm.Task.Status != ProjectTasksStatus.Completed &&
                    tm.Task.Status != ProjectTasksStatus.Cancelled)
                .GroupBy(tm => tm.UserId)
                .Select(g => new
                {
                    UserId = g.Key,
                    Count = g.Select(x => x.TaskId)
                             .Distinct()
                             .Count()
                })
                .ToDictionaryAsync(
                    x => x.UserId,
                    x => x.Count);
        }

        // Get progress notes
        public async Task<IReadOnlyList<TaskProgressNote>> GetProgressNotesAsync(
            int taskId)
        {
            return await _context.Set<TaskProgressNote>()
                .Include(n => n.AuthorUser)
                .AsNoTracking()
                .Where(n => n.TaskId == taskId)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();
        }

        public async Task AddProgressNoteAsync(TaskProgressNote note)
        {
            await _context.Set<TaskProgressNote>().AddAsync(note);
        }

        // Count overdue tasks
        public async Task<int> CountOverdueAsync(int? companyId)
        {
            var today = DateTime.UtcNow.Date;

            var query = _context.Tasks
                .AsNoTracking()
                .Where(t =>
                    t.DueDate != null &&
                    t.DueDate < today &&
                    t.Status != ProjectTasksStatus.Completed &&
                    t.Status != ProjectTasksStatus.Cancelled);

            if (companyId.HasValue)
            {
                query = query.Where(t =>
                    t.Project.CompanyId == companyId.Value);
            }

            return await query.CountAsync();
        }

        // Get tasks due today
        public async Task<IReadOnlyList<ProjectTask>> GetDueTodayAsync(
            int? companyId,
            int? userId)
        {
            var today = DateTime.UtcNow.Date;

            var query = _context.Tasks
                .AsNoTracking()
                .Where(t =>
                    t.DueDate != null &&
                    t.DueDate.Value.Date == today &&
                    t.Status != ProjectTasksStatus.Completed &&
                    t.Status != ProjectTasksStatus.Cancelled);

            if (companyId.HasValue)
            {
                query = query.Where(t =>
                    t.Project.CompanyId == companyId.Value);
            }

            // Multiple members supported
            if (userId.HasValue)
            {
                query = query.Where(t =>
                    t.TaskMembers.Any(tm => tm.UserId == userId.Value));
            }

            return await query
                .OrderBy(t => t.Priority)
                .ToListAsync();
        }

        // Get recently created tasks
        public async Task<IReadOnlyList<ProjectTask>> GetRecentAsync(
            int companyId,
            int count)
        {
            return await _context.Tasks
                .AsNoTracking()
                .Where(t => t.Project.CompanyId == companyId)
                .OrderByDescending(t => t.CreatedAt)
                .Take(count)
                .ToListAsync();
        }

        // Filter tasks
        public async Task<(IReadOnlyList<ProjectTask> Items, int TotalCount)>
            FilterAsync(
                int? assignedToUserId,
                TaskPriority? priority,
                ProjectTasksStatus? status,
                int? projectId,
                int? companyId,
                int? enforcedCompanyId,
                int pageNumber,
                int pageSize)
        {
            var query = _context.Tasks
                .AsNoTracking()
                .AsQueryable();

            // Manager enforced company scope
            if (enforcedCompanyId.HasValue)
            {
                query = query.Where(t =>
                    t.Project.CompanyId == enforcedCompanyId.Value);
            }

            // Company filter
            if (companyId.HasValue)
            {
                query = query.Where(t =>
                    t.Project.CompanyId == companyId.Value);
            }

            // Project filter
            if (projectId.HasValue)
            {
                query = query.Where(t =>
                    t.ProjectId == projectId.Value);
            }

            // Multiple members supported
            if (assignedToUserId.HasValue)
            {
                query = query.Where(t =>
                    t.TaskMembers.Any(tm =>
                        tm.UserId == assignedToUserId.Value));
            }

            // Priority filter
            if (priority.HasValue)
            {
                query = query.Where(t =>
                    t.Priority == priority.Value);
            }

            // Status filter
            if (status.HasValue)
            {
                query = query.Where(t =>
                    t.Status == status.Value);
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

        // Employee performance based on TaskMembers
        public async Task<IReadOnlyList<(
            int UserId,
            int TotalAssigned,
            int Completed)>>
            GetEmployeePerformanceAsync(int companyId)
        {
            var grouped = await _context.TaskMembers
                .AsNoTracking()
                .Where(tm =>
                    tm.Task.Project.CompanyId == companyId)
                .GroupBy(tm => tm.UserId)
                .Select(g => new
                {
                    UserId = g.Key,

                    TotalAssigned = g.Select(x => x.TaskId)
                                     .Distinct()
                                     .Count(),

                    Completed = g
                        .Where(x =>
                            x.Task.Status ==
                            ProjectTasksStatus.Completed)
                        .Select(x => x.TaskId)
                        .Distinct()
                        .Count()
                })
                .ToListAsync();

            return grouped
                .Select(x =>
                    (x.UserId,
                     x.TotalAssigned,
                     x.Completed))
                .ToList();
        }
        public async Task<Dictionary<int, int>> GetOverdueTaskCountsByUserAsync(
    int companyId)
        {
            var today = DateTime.UtcNow.Date;

            return await _context.TaskMembers
                .AsNoTracking()
                .Where(tm =>
                    tm.Task.Project.CompanyId == companyId &&
                    tm.Task.DueDate != null &&
                    tm.Task.DueDate < today &&
                    tm.Task.Status != ProjectTasksStatus.Completed &&
                    tm.Task.Status != ProjectTasksStatus.Cancelled)
                .GroupBy(tm => tm.UserId)
                .Select(g => new
                {
                    UserId = g.Key,
                    Count = g.Count()
                })
                .ToDictionaryAsync(
                    x => x.UserId,
                    x => x.Count);
        }
    }
}