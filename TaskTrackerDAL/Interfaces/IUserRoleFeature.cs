using System;
using System.Collections.Generic;
using System.Text;
using TaskTrackerDAL.Interfaces.Generic;
using TaskTrackerDAL.Models;

namespace TaskTrackerDAL.Interfaces
{
    public interface IUserRoleFeature:IGenericFeature<UserRole>
    {
        //Task<User?> GetByIdWithRolesAsync(int userId);
        Task DeleteAsync(int userId, int roleId);
        Task<UserRole?> GetAsync(int userId, int roleId);
    }
}
