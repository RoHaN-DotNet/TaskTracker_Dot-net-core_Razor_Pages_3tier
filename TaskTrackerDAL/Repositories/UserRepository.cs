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
    public class UserRepository : GenericRepository<User>, IUserFeature
    {
        public UserRepository(TaskTrackerDbContext context) : base(context)
        {
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _context.Users
                .AsNoTracking()
                .SingleOrDefaultAsync(u => u.Email == email);
        }

        public async Task<User?> GetByUserNameAsync(string userName)
        {
            return await _context.Users
                .AsNoTracking()
                .SingleOrDefaultAsync(u => u.UserName == userName);
        }

        public async Task<User?> GetByIdWithRolesAsync(int userId)
        {
            return await _context.Users
                .AsNoTracking()
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .SingleOrDefaultAsync(u => u.Id == userId);
        }

        public async Task<IReadOnlyList<User>> GetByCompanyIdAsync(int companyId)
        {
            return await _context.Users
                .AsNoTracking()
                .Where(u => u.CompanyId == companyId)
                .OrderBy(u => u.FullName)
                .ToListAsync();
        }

        public async Task<bool> IsEmailUniqueAsync(
    string email,
    int? excludeUserId = null)
        {
            var query = _context.Users
                .AsNoTracking()
                .Where(u => u.Email == email);

            if (excludeUserId.HasValue)
            {
                query = query.Where(
                    u => u.Id != excludeUserId.Value);
            }

            return !await query.AnyAsync();
        }

        public async Task<bool> IsUserNameUniqueAsync(string userName, int? excludeUserId = null)
        {
            var query = _context.Users.AsNoTracking().Where(u => u.UserName == userName);

            if (excludeUserId.HasValue)
            {
                query = query.Where(u => u.Id != excludeUserId.Value);
            }

            return !await query.AnyAsync();
        }

        public async Task<IReadOnlyList<User>> SearchEmployeesAsync(
            int? companyId,
            string? searchTerm,
            string? roleName,
            bool? isActive)
        {
            var query = _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .AsNoTracking()
                .Where(u => u.UserRoles.Any(ur =>
                    ur.Role.Name == "Developer" ||
                    ur.Role.Name == "Tester" ||
                    ur.Role.Name == "Debugger" ||
                    ur.Role.Name == "UI/UX" ||
                    ur.Role.Name == "Team Lead"||
                    ur.Role.Name == "Manager" ||
                    ur.Role.Name == "Designer"));

            if (companyId.HasValue)
            {
                query = query.Where(u => u.CompanyId == companyId.Value);
            }

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim();
                query = query.Where(u =>
                    u.FullName.Contains(term) ||
                    u.Email.Contains(term) ||
                    u.UserName.Contains(term));
            }

            if (!string.IsNullOrWhiteSpace(roleName))
            {
                query = query.Where(u => u.UserRoles.Any(ur => ur.Role.Name == roleName));
            }

            if (isActive.HasValue)
            {
                query = query.Where(u => u.IsActive == isActive.Value);
            }

            return await query
                .OrderBy(u => u.FullName)
                .ToListAsync();
        }
        public async Task<int> CountByRoleNamesAsync(IEnumerable<string> roleNames)
        {
            var names = roleNames.ToList();

            return await _context.Users
                .AsNoTracking()
                .CountAsync(u => u.UserRoles.Any(Uri => names.Contains(Uri.Role.Name)));

        }
        public async Task UpdateEmployeeAsync(
    User employee,
    int? oldRoleId,
    int newRoleId)
        {
            // =========================================================
            // IMPORTANT:
            // Do NOT use:
            //
            // _context.Users.Update(employee);
            // _context.Users.Attach(employee);
            //
            // because employee contains UserRoles navigation.
            // =========================================================


            // =========================================================
            // Attach ONLY the User entity
            // =========================================================

            _context.Entry(employee).State = EntityState.Unchanged;

            _context.Entry(employee)
                .Property(u => u.FullName)
                .IsModified = true;

            _context.Entry(employee)
                .Property(u => u.Email)
                .IsModified = true;

            _context.Entry(employee)
                .Property(u => u.UserName)
                .IsModified = true;

            _context.Entry(employee)
                .Property(u => u.UpdatedAt)
                .IsModified = true;


            // =========================================================
            // Role changed
            // =========================================================

            if (oldRoleId.HasValue &&
                oldRoleId.Value != newRoleId)
            {
                // Delete old UserRole directly from database.
                // We don't Attach the old UserRole.
                await _context.UserRoles
                    .Where(ur =>
                        ur.UserId == employee.Id &&
                        ur.RoleId == oldRoleId.Value)
                    .ExecuteDeleteAsync();


                // Add new UserRole
                await _context.UserRoles.AddAsync(
                    new UserRole
                    {
                        UserId = employee.Id,
                        RoleId = newRoleId
                    });
            }


            // =========================================================
            // Employee has no role
            // =========================================================

            else if (!oldRoleId.HasValue)
            {
                await _context.UserRoles.AddAsync(
                    new UserRole
                    {
                        UserId = employee.Id,
                        RoleId = newRoleId
                    });
            }
        }
    }

}
