using System;
using System.Collections.Generic;
using System.Text;
using TaskTrackerBLL.DTOs.Role;
using TaskTrackerBLL.DTOs.Employee;
using TaskTrackerBLL.Common;

namespace TaskTrackerBLL.Interfaces.Services
{
    public interface IRoleAssignmentService
    {
        Task<Result<IReadOnlyList<AssignableRoleDto>>> GetAssignableRolesAsync(bool actingUserIsAdmin);
        Task<Result> AssignRoleAsync(
            UpdateEmployeeRoleDto dto,
            bool actingUserIsAdmin,
            int? actingManagerCompanyId
            );
    }
}
