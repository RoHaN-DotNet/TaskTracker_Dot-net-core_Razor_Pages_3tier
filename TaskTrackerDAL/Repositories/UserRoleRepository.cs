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
    public class UserRoleRepository : GenericRepository<UserRole>,IUserRoleFeature
    {
        public UserRoleRepository(TaskTrackerDbContext context) : base(context)
        {
        }
        public async Task<UserRole?> GetAsync(int userId, int roleId)
        {
            return await _context.UserRoles
                .SingleOrDefaultAsync(
                    ur => ur.UserId == userId &&
                          ur.RoleId == roleId);
        }
        public async Task DeleteAsync(int userId, int roleId)
        {
            var userRole = await _context.UserRoles
                .SingleOrDefaultAsync(
                    ur => ur.UserId == userId &&
                          ur.RoleId == roleId);

            if (userRole is not null)
            {
                _context.UserRoles.Remove(userRole);
            }
        }
    }
}
