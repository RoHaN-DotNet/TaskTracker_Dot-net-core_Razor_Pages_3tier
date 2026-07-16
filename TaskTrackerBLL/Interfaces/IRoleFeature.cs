using TaskTrackerDAL.Interfaces.Generic;
using TaskTrackerDAL.Models;
namespace TaskTrackerBLL.Interfaces
{
    public interface IRoleFeature:IGenericFeature<Role>
    {
        Task<Role?> GetByNameAsync(string name);

        Task<bool> IsNameUniqueAsync(string name, int? excludeRoleId = null);

        Task<int> CountAssignedUsersAsync(int roleId);

        Task<IReadOnlyList<Role>> GetByNamesAsync(IEnumerable<string> names);
    }
}
