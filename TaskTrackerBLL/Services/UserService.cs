using System;
using System.Collections.Generic;
using System.Text;
using TaskTrackerBLL.Common;
using TaskTrackerBLL.DTOs.User;
using TaskTrackerDAL.Models;

namespace TaskTrackerBLL.Interfaces.Services
{
    public class UserService : IUserService
    {
        private readonly IUnitOfWork _unitOfWork;

        public UserService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<UserDto>> GetByIdAsync(int id)
        {
            var user = await _unitOfWork.Users.GetByIdWithRolesAsync(id);

            if (user is null)
            {
                return Result<UserDto>.Failure($"User with ID {id} was not found.");
            }

            return Result<UserDto>.Success(MapToDto(user));
        }

        public async Task<Result<IReadOnlyList<UserDto>>> GetByCompanyIdAsync(int companyId)
        {
            var users = await _unitOfWork.Users.GetByCompanyIdAsync(companyId);

            var dtos = new List<UserDto>();

            foreach (var user in users)
            {
                var withRoles = await _unitOfWork.Users.GetByIdWithRolesAsync(user.Id);
                if (withRoles is not null)
                {
                    dtos.Add(MapToDto(withRoles));
                }
            }

            return Result<IReadOnlyList<UserDto>>.Success(dtos);
        }

        public async Task<Result> UpdateAsync(UpdateUserDto dto)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(dto.Id);

            if (user is null)
            {
                return Result.Failure($"User with ID {dto.Id} was not found.");
            }

            var isEmailUnique = await _unitOfWork.Users.IsEmailUniqueAsync(dto.Email, dto.Id);
            if (!isEmailUnique)
            {
                return Result.Failure($"Email '{dto.Email}' is already in use.");
            }

            var isUserNameUnique = await _unitOfWork.Users.IsUserNameUniqueAsync(dto.UserName, dto.Id);
            if (!isUserNameUnique)
            {
                return Result.Failure($"Username '{dto.UserName}' is already in use.");
            }

            user.FullName = dto.FullName;
            user.Email = dto.Email;
            user.UserName = dto.UserName;
            user.IsActive = dto.IsActive;
            user.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Users.Update(user);
            await _unitOfWork.SaveChangesAsync();

            return Result.Success();
        }

        public async Task<Result> DeactivateAsync(int id)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(id);

            if (user is null)
            {
                return Result.Failure($"User with ID {id} was not found.");
            }

            user.IsActive = false;
            user.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Users.Update(user);
            await _unitOfWork.SaveChangesAsync();

            return Result.Success();
        }

        public async Task<Result> AssignRoleAsync(int userId, int roleId)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId);
            if (user is null)
            {
                return Result.Failure($"User with ID {userId} was not found.");
            }

            var role = await _unitOfWork.Roles.GetByIdAsync(roleId);
            if (role is null)
            {
                return Result.Failure($"Role with ID {roleId} was not found.");
            }

            var userWithRoles = await _unitOfWork.Users.GetByIdWithRolesAsync(userId);

            if (userWithRoles!.UserRoles.Any(ur => ur.RoleId == roleId))
            {
                return Result.Failure($"User already has the '{role.Name}' role.");
            }

            var userRole = new UserRole
            {
                UserId = userId,
                RoleId = roleId
            };

            user.UserRoles.Add(userRole);
            _unitOfWork.Users.Update(user);
            await _unitOfWork.SaveChangesAsync();

            return Result.Success();
        }

        public async Task<Result> RemoveRoleAsync(int userId, int roleId)
        {
            var userWithRoles = await _unitOfWork.Users.GetByIdWithRolesAsync(userId);

            if (userWithRoles is null)
            {
                return Result.Failure($"User with ID {userId} was not found.");
            }

            var existingUserRole = userWithRoles.UserRoles.FirstOrDefault(ur => ur.RoleId == roleId);

            if (existingUserRole is null)
            {
                return Result.Failure("User does not have this role assigned.");
            }

            userWithRoles.UserRoles.Remove(existingUserRole);
            _unitOfWork.Users.Update(userWithRoles);
            await _unitOfWork.SaveChangesAsync();

            return Result.Success();
        }

        private static UserDto MapToDto(User user)
        {
            return new UserDto
            {
                Id = user.Id,
                CompanyId = user.CompanyId,
                FullName = user.FullName,
                Email = user.Email,
                UserName = user.UserName,
                IsActive = user.IsActive,
                LastLoginAt = user.LastLoginAt,
                Roles = user.UserRoles.Select(ur => ur.Role.Name).ToList()
            };
        }
    }
}
