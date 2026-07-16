using System;
using System.Collections.Generic;
using System.Text;
using TaskTrackerBLL.Common;
using TaskTrackerBLL.DTOs.Role;

namespace TaskTrackerBLL.Interfaces.Services
{
    public interface IRoleService
    {
        Task<Result<RoleDto>> GetByIdAsync(int id);

        Task<Result<IReadOnlyList<RoleDto>>> GetAllAsync();

        Task<Result<RoleDto>> CreateAsync(CreateRoleDto dto);

        Task<Result> UpdateAsync(UpdateRoleDto dto);

        Task<Result> DeleteAsync(int id);
    }
}
