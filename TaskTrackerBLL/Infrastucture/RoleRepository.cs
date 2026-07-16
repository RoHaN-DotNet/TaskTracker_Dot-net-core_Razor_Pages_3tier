
using Microsoft.EntityFrameworkCore;
using TaskTrackerBLL.Interfaces;
using TaskTrackerDAL.Data;
using TaskTrackerDAL.Models;
using TaskTrackerDAL.Repositories.Generic;

namespace TaskTrackerBLL.Infrastucture
{
    public class RoleRepository : GenericRepository<Role>, IRoleFeature
    {
        public RoleRepository(TaskTrackerDbContext context) : base(context)
        {
        }

        public async Task<Role?> GetByNameAsync(string name)
        {
            return await _context.Roles
                .AsNoTracking()
                .SingleOrDefaultAsync(r => r.Name == name);
        }

        public async Task<bool> IsNameUniqueAsync(string name, int? excludeRoleId = null)
        {
            var query = _context.Roles.AsNoTracking().Where(r => r.Name == name);

            if (excludeRoleId.HasValue)
            {
                query = query.Where(r => r.Id != excludeRoleId.Value);
            }

            return !await query.AnyAsync();
        }

        public async Task<int> CountAssignedUsersAsync(int roleId)
        {
            return await _context.UserRoles
                .AsNoTracking()
                .CountAsync(ur => ur.RoleId == roleId);
        }

        public async Task<IReadOnlyList<Role>> GetByNamesAsync(IEnumerable<string> names)
        {
            var nameList = names.ToList();

            return await _context.Roles
                .AsNoTracking()
                .Where(r => nameList.Contains(r.Name))
                .OrderBy(r => r.Name)
                .ToListAsync();
        }
    }
}
