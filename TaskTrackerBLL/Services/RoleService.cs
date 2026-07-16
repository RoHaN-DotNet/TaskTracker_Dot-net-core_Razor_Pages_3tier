using System;
using System.Collections.Generic;
using System.Text;
using TaskTrackerBLL.Common;
using TaskTrackerBLL.DTOs.Role;
using TaskTrackerBLL.Interfaces;
using TaskTrackerBLL.Interfaces.Services;
using TaskTrackerDAL.Models;

namespace TaskTrackerBLL.Services
{
    public class RoleService:IRoleService
    {
        private readonly IUnitOfWork _unitOfWork;

        public RoleService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        //Get Role by Id
        public async Task<Result<RoleDto>> GetByIdAsync(int id)
        {
            var role = await _unitOfWork.Roles.GetByIdAsync(id);

            if (role is null)
            {
                return Result<RoleDto>.Failure($"Role with ID {id} was not found.");
            }

            var assignedCount = await _unitOfWork.Roles.CountAssignedUsersAsync(id);

            return Result<RoleDto>.Success(MapToDto(role, assignedCount));
        }
        //Gets all the roles
        public async Task<Result<IReadOnlyList<RoleDto>>> GetAllAsync()
        {
            var roles = await _unitOfWork.Roles.GetAllAsync();

            var dtos = new List<RoleDto>();

            foreach (var role in roles)
            {
                var assignedCount = await _unitOfWork.Roles.CountAssignedUsersAsync(role.Id);
                dtos.Add(MapToDto(role, assignedCount));
            }

            return Result<IReadOnlyList<RoleDto>>.Success(dtos);
        }

        //create role
        public async Task<Result<RoleDto>> CreateAsync(CreateRoleDto dto)
        {
            var isUnique = await _unitOfWork.Roles.IsNameUniqueAsync(dto.Name);

            if (!isUnique)
            {
                return Result<RoleDto>.Failure($"A role named '{dto.Name}' already exists.");
            }

            var role = new Role
            {
                Name = dto.Name,
                Description = dto.Description
            };

            await _unitOfWork.Roles.AddAsync(role);
            await _unitOfWork.SaveChangesAsync();

            return Result<RoleDto>.Success(MapToDto(role, 0));
        }
        //Update role
        public async Task<Result> UpdateAsync(UpdateRoleDto dto)
        {
            var role = await _unitOfWork.Roles.GetByIdAsync(dto.Id);

            if (role is null)
            {
                return Result.Failure($"Role with ID {dto.Id} was not found.");
            }

            var isUnique = await _unitOfWork.Roles.IsNameUniqueAsync(dto.Name, dto.Id);

            if (!isUnique)
            {
                return Result.Failure($"A role named '{dto.Name}' already exists.");
            }

            role.Name = dto.Name;
            role.Description = dto.Description;
            role.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Roles.Update(role);
            await _unitOfWork.SaveChangesAsync();

            return Result.Success();
        }
        //Delete role
        public async Task<Result> DeleteAsync(int id)
        {
            var role = await _unitOfWork.Roles.GetByIdAsync(id);

            if (role is null)
            {
                return Result.Failure($"Role with ID {id} was not found.");
            }

            var assignedCount = await _unitOfWork.Roles.CountAssignedUsersAsync(id);

            if (assignedCount > 0)
            {
                return Result.Failure(
                    $"Cannot delete role '{role.Name}' because it is currently assigned to {assignedCount} user(s).");
            }

            _unitOfWork.Roles.Remove(role);
            await _unitOfWork.SaveChangesAsync();

            return Result.Success();
        }

        private static RoleDto MapToDto(Role role, int assignedCount)
        {
            return new RoleDto
            {
                Id = role.Id,
                Name = role.Name,
                Description = role.Description,
                TotalUsersAssigned = assignedCount
            };
        }
    }
}
