using System;
using System.Collections.Generic;
using System.Text;
using TaskTrackerBLL.Common;
using TaskTrackerBLL.DTOs.Profile;
using TaskTrackerBLL.DTOs.User;

namespace TaskTrackerBLL.Interfaces.Services
{
    public interface IUserService
    {
        Task<Result<UserDto>> GetByIdAsync(int id);

        Task<Result<IReadOnlyList<UserDto>>> GetByCompanyIdAsync(int companyId);

        Task<Result> UpdateAsync(UpdateUserDto dto);

        Task<Result> DeactivateAsync(int id);

        Task<Result> AssignRoleAsync(int userId, int roleId);

        Task<Result> RemoveRoleAsync(int userId, int roleId);

        Task<Result<ProfileDto>> GetProfileAsync(int id);
        Task<Result> UpdateProfileAsync(UpdateProfileDto input);

        Task<Result> ChangePasswordAsync(
            int userId,
            string currentPassword,
            string newPassword);
    }
}
