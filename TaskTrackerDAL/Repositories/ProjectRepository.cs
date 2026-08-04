using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using TaskTrackerDAL.Data;
using TaskTrackerDAL.Interfaces;
using TaskTrackerDAL.Models;
using TaskTrackerDAL.Models.Enums;
using TaskTrackerDAL.Repositories.Generic;

namespace TaskTrackerDAL.Repositories
{
    public class ProjectRepository:GenericRepository<Project>,IProjectFeature
    {
        public ProjectRepository(TaskTrackerDbContext context) : base(context)
        {
        }

        public async Task<Project?> GetByIdWithMembersAsync(int projectId)
        {
            return await _context.Projects
                .Include(p => p.ProjectMembers)
                    .ThenInclude(pm => pm.User)
                //.AsNoTracking()
                .SingleOrDefaultAsync(p => p.Id == projectId);
        }

        public async Task<Project?> GetByIdWithTasksAsync(int projectId)
        {
            return await _context.Projects
                .Include(p => p.Tasks)
                .AsNoTracking()
                .SingleOrDefaultAsync(p => p.Id == projectId);
        }

        public async Task<IReadOnlyList<Project>> GetByCompanyIdAsync(int companyId)
        {
            return await _context.Projects
                .AsNoTracking()
                .Where(p => p.CompanyId == companyId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<IReadOnlyList<Project>> GetByMemberUserIdAsync(int userId)
        {
            return await _context.Projects
    .Include(p => p.ProjectMembers)
        .ThenInclude(pm => pm.User)
    .AsNoTracking()
    .Where(p => p.ProjectMembers.Any(pm => pm.UserId == userId))
    .OrderByDescending(p => p.CreatedAt)
    .ToListAsync();
        }

        public async Task<int> CountByStatusAsync(int companyId, ProjectStatus status)
        {
            return await _context.Projects
                .AsNoTracking()
                .CountAsync(p => p.CompanyId == companyId && p.Status == status);
        }

        public async Task<bool> IsUserProjectMemberAsync(int projectId, int userId)
        {
            return await _context.ProjectMembers
                .AsNoTracking()
                .AnyAsync(pm => pm.ProjectId == projectId && pm.UserId == userId);
        }

        public async Task<(IReadOnlyList<Project> Items, int TotalCount)> SearchByCompanyAsync(
            int? companyId,
            string? searchTerm,
            ProjectStatus? status,
            int pageNumber,
            int pageSize)
        {
            var query = _context.Projects
    .Include(p => p.ProjectMembers)
        .ThenInclude(pm => pm.User)
    .AsNoTracking()
    .AsQueryable();

            if (companyId.HasValue)
            {
                query = query.Where(p => p.CompanyId == companyId.Value);
            }

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim();
                query = query.Where(p => p.Name.Contains(term));
            }

            if (status.HasValue)
            {
                query = query.Where(p => p.Status == status.Value);
            }

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(p => p.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }
        public async Task<(IReadOnlyList<Project>Items, int TotalCount)> FilterAsync(
            int? companyId,
    ProjectStatus? status,
    TaskPriority? priority,
    DateTime? deadlineFrom,
    DateTime? deadlineTo,
    int? enforcedCompanyId,
    int pageNumber,
    int pageSize)
        {
            var query=_context.Projects.AsNoTracking().AsQueryable();
            // Authorization boundary — applied first, always, regardless of the
            // person's own filter selection.
            if (enforcedCompanyId.HasValue)
            {
                query = query.Where(p => p.CompanyId == enforcedCompanyId.Value);
            }
            if (companyId.HasValue)
            {
                query = query.Where(p => p.CompanyId == companyId.Value);
            }
            // User-chosen filters — narrow further within whatever the boundary above allows.
            if (companyId.HasValue)
            {
                query = query.Where(p => p.Status == status!.Value); 

            }
            if (status.HasValue) 
            {
                query = query.Where(p => p.Status == status.Value);
            }
            if (priority.HasValue)
            {
                query = query.Where(p => p.Priority == priority.Value);
            }
            if(deadlineFrom.HasValue)
            {
                query = query.Where(p => p.EndDate != null && p.EndDate >= deadlineFrom.Value.Date);
            }
            if (deadlineTo.HasValue)
            {
                query = query.Where(p => p.EndDate != null && p.EndDate <= deadlineTo.Value.Date);
            }
            var totalCount = await query.CountAsync();

            var items = await query
                .OrderBy(p => p.EndDate ?? DateTime.MaxValue)
                .ThenByDescending(p => p.Priority)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }



    }
}
