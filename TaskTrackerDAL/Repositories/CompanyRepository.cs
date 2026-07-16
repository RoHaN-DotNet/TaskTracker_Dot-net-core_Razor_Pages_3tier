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
    public class CompanyRepository:GenericRepository<Company>, ICompanyFeature
    {
        public CompanyRepository(TaskTrackerDbContext context) : base(context)
        {


        }
        public async Task<Company?> GetByIdWithUsersAsync(int companyId)
        {
            return await _context.Companies
                .Include(c => c.Users)
                .AsNoTracking()
                .SingleOrDefaultAsync(c => c.Id == companyId);
        }

        public async Task<Company?> GetByIdWithProjectsAsync(int companyId)
        {
            return await _context.Companies
                .Include(c => c.Projects)
                .AsNoTracking()
                .SingleOrDefaultAsync(c => c.Id == companyId);
        }

        public async Task<bool> IsNameUniqueAsync(string name, int? excludeCompanyId = null)
        {
            var query = _context.Companies.AsNoTracking().Where(c => c.Name == name);

            if (excludeCompanyId.HasValue)
            {
                query = query.Where(c => c.Id != excludeCompanyId.Value);
            }

            return !await query.AnyAsync();
        }

        public async Task<IReadOnlyList<Company>> GetActiveCompaniesAsync()
        {
            return await _context.Companies
                .AsNoTracking()
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .ToListAsync();
        }

        public async Task<(IReadOnlyList<Company> Items, int TotalCount)> SearchAsync(
            string? searchTerm,
            bool? isActive,
            int pageNumber,
            int pageSize)
        {
            var query = _context.Companies.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim();
                query = query.Where(c =>
                    c.Name.Contains(term) ||
                    (c.Email != null && c.Email.Contains(term)));
            }

            if (isActive.HasValue)
            {
                query = query.Where(c => c.IsActive == isActive.Value);
            }

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderBy(c => c.Name)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<bool> HasAnyUsersOrProjectsAsync(int companyId)
        {
            var hasUsers = await _context.Users.AnyAsync(u => u.CompanyId == companyId);
            var hasProjects = await _context.Projects.AnyAsync(p => p.CompanyId == companyId);

            return hasUsers || hasProjects;
        }
    }
}
