using System;
using System.Collections.Generic;
using System.Text;
using TaskTrackerDAL.Models;
using TaskTrackerBLL.DTOs.Role;

using TaskTrackerBLL.Interfaces;
using TaskTrackerBLL.Common;
using TaskTrackerBLL.Interfaces.Services;
using TaskTrackerDAL.Constants;
using TaskTrackerBLL.DTOs.Employee;
using System.Net.WebSockets;

namespace TaskTrackerBLL.Services
{
    public class RoleAssignmentService:IRoleAssignmentService
    {
        public readonly IUnitOfWork _unitOfWork;

        public RoleAssignmentService(IUnitOfWork unitOfwork)
        {
            _unitOfWork = unitOfwork;
        }
        public async Task<Result<IReadOnlyList<AssignableRoleDto>>> GetAssignableRolesAsync(bool actingUserIsAdmin)
        {
            var roleNames = actingUserIsAdmin
                ? AppRoles.All
                : AppRoles.EmployeeRoles;

            var roles = await _unitOfWork.Roles.GetByNamesAsync(roleNames);

            var dtos = roles
                .Select(r => new AssignableRoleDto { Id = r.Id, Name = r.Name }).ToList();
            return Result<IReadOnlyList<AssignableRoleDto>>.Success(dtos);
        }
        public async Task<Result> AssignRoleAsync(
            UpdateEmployeeRoleDto dto,
            bool actingUserIsAdmin,
            int? actingManagerCompanyId)
        {
            if(!actingUserIsAdmin && dto.NewRoleName == AppRoles.Admin)
            {
                return Result.Failure("Managers are not permitted to assign the Admin role");
            }
            if(!actingUserIsAdmin && dto.NewRoleName == AppRoles.Manager)
            {
                return Result.Failure("Managers are not permitted to assign the Manager Role");
            }
            var employee = await _unitOfWork.Users.GetByIdWithRolesAsync(dto.EmployeeId);
            if(employee is null || !employee.UserRoles.Any(ur => AppRoles.EmployeeRoles.Contains(ur.Role.Name)))
            {
                return Result.Failure($"Employee with {dto.EmployeeId} was not found.");
            }
            if(actingManagerCompanyId.HasValue && employee.CompanyId != actingManagerCompanyId.Value)
            {
                return Result.Failure("You are not authorized to change this employee's role.");
            }
            var newRole = await _unitOfWork.Roles.GetByNameAsync(dto.NewRoleName);
            if(newRole is null)
            {
                return Result.Failure($"Role {dto.NewRoleName} has not been configured.");
            }
            var currentRoleLink = employee.UserRoles.FirstOrDefault(ur => AppRoles.EmployeeRoles.Contains(ur.Role.Name));

            if(currentRoleLink is not null && currentRoleLink.RoleId == newRole.Id)
            {
                return Result.Failure($"{employee.FullName} already holds the'{newRole.Name}' role");
            }
            if(currentRoleLink is not null)
            {
                employee.UserRoles.Remove(currentRoleLink);
            }
            employee.UserRoles.Add(new UserRole
            {
                UserId = employee.Id,
                RoleId=newRole.Id
            });
            employee.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Users.Update(employee);
            await _unitOfWork.SaveChangesAsync();

            return Result.Success();
        }
            
        }
    }


