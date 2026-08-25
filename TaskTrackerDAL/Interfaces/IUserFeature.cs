using TaskTrackerDAL.Interfaces.Generic;
using TaskTrackerDAL.Models;

namespace TaskTrackerDAL.Interfaces
{
    public interface IUserFeature : IGenericFeature<User>
    {
        Task<User?> GetByEmailAsync(string email);

        Task<User?> GetByUserNameAsync(string userName);

        Task<User?> GetByIdWithRolesAsync(int userId);

        Task<IReadOnlyList<User>> GetByCompanyIdAsync(int companyId);

        Task<bool> IsEmailUniqueAsync(string email, int? excludeUserId = null);

        Task<bool> IsUserNameUniqueAsync(string userName, int? excludeUserId = null);
        Task UpdateEmployeeAsync(User employee, int? oldRoleId,int newRoleId);
        Task<IReadOnlyList<User>> SearchEmployeesAsync(
            int? companyId,
            string? searchTerm,
            string? roleName,
            bool? isActive);
        Task<int> CountByRoleNamesAsync(IEnumerable<string> roleNames);
    }
}
