using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TaskTrackerBLL.Common;
using TaskTrackerBLL.DTOs.Profile;
using TaskTrackerBLL.DTOs.User;
using TaskTrackerBLL.Interfaces.Security;
using TaskTrackerDAL.Models;
using TaskTrackerDAL.Repositories;

namespace TaskTrackerBLL.Interfaces.Services
{
    public class UserService : IUserService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPasswordHasher _passwordHasher;

        public UserService(
            IUnitOfWork unitOfWork,
            IPasswordHasher passwordHasher)
        {
            _unitOfWork = unitOfWork;
            _passwordHasher = passwordHasher;
        }

        // =========================================================
        // GET USER BY ID
        // =========================================================

        public async Task<Result<UserDto>> GetByIdAsync(int id)
        {
            var user = await _unitOfWork.Users.GetByIdWithRolesAsync(id);

            if (user is null)
            {
                return Result<UserDto>.Failure(
                    $"User with ID {id} was not found.");
            }

            return Result<UserDto>.Success(
                MapToDto(user));
        }


        // =========================================================
        // GET USERS BY COMPANY
        // =========================================================

        public async Task<Result<IReadOnlyList<UserDto>>> GetByCompanyIdAsync(
            int companyId)
        {
            var users =
                await _unitOfWork.Users.GetByCompanyIdAsync(companyId);

            var dtos = new List<UserDto>();

            foreach (var user in users)
            {
                var withRoles =
                    await _unitOfWork.Users.GetByIdWithRolesAsync(user.Id);

                if (withRoles is not null)
                {
                    dtos.Add(MapToDto(withRoles));
                }
            }

            return Result<IReadOnlyList<UserDto>>.Success(dtos);
        }


        // =========================================================
        // UPDATE USER
        // =========================================================

        public async Task<Result> UpdateAsync(UpdateUserDto dto)
        {
            var user =
                await _unitOfWork.Users.GetByIdAsync(dto.Id);

            if (user is null)
            {
                return Result.Failure(
                    $"User with ID {dto.Id} was not found.");
            }


            // -----------------------------------------------------
            // Email uniqueness
            // -----------------------------------------------------

            var isEmailUnique =
                await _unitOfWork.Users.IsEmailUniqueAsync(
                    dto.Email,
                    dto.Id);

            if (!isEmailUnique)
            {
                return Result.Failure(
                    $"Email '{dto.Email}' is already in use.");
            }


            // -----------------------------------------------------
            // Username uniqueness
            // -----------------------------------------------------

            var isUserNameUnique =
                await _unitOfWork.Users.IsUserNameUniqueAsync(
                    dto.UserName,
                    dto.Id);

            if (!isUserNameUnique)
            {
                return Result.Failure(
                    $"Username '{dto.UserName}' is already in use.");
            }


            // -----------------------------------------------------
            // Update fields
            // -----------------------------------------------------

            user.FullName = dto.FullName;
            user.Email = dto.Email;
            user.UserName = dto.UserName;

            // Do NOT change IsActive here

            user.UpdatedAt = DateTime.UtcNow;


            // -----------------------------------------------------
            // Update database
            // -----------------------------------------------------

            _unitOfWork.Users.Update(user);

            await _unitOfWork.SaveChangesAsync();

            return Result.Success();
        }


        // =========================================================
        // DEACTIVATE USER
        // =========================================================

        public async Task<Result> DeactivateAsync(int id)
        {
            var user =
                await _unitOfWork.Users.GetByIdAsync(id);

            if (user is null)
            {
                return Result.Failure(
                    $"User with ID {id} was not found.");
            }

            user.IsActive = false;
            user.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Users.Update(user);

            await _unitOfWork.SaveChangesAsync();

            return Result.Success();
        }


        // =========================================================
        // ASSIGN ROLE
        // =========================================================

        public async Task<Result> AssignRoleAsync(
            int userId,
            int roleId)
        {
            var user =
                await _unitOfWork.Users.GetByIdAsync(userId);

            if (user is null)
            {
                return Result.Failure(
                    $"User with ID {userId} was not found.");
            }


            var role =
                await _unitOfWork.Roles.GetByIdAsync(roleId);

            if (role is null)
            {
                return Result.Failure(
                    $"Role with ID {roleId} was not found.");
            }


            var userWithRoles =
                await _unitOfWork.Users.GetByIdWithRolesAsync(userId);

            if (userWithRoles!.UserRoles.Any(
                    ur => ur.RoleId == roleId))
            {
                return Result.Failure(
                    $"User already has the '{role.Name}' role.");
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


        // =========================================================
        // REMOVE ROLE
        // =========================================================

        public async Task<Result> RemoveRoleAsync(
            int userId,
            int roleId)
        {
            var userWithRoles =
                await _unitOfWork.Users.GetByIdWithRolesAsync(userId);

            if (userWithRoles is null)
            {
                return Result.Failure(
                    $"User with ID {userId} was not found.");
            }


            var existingUserRole =
                userWithRoles.UserRoles
                    .FirstOrDefault(ur => ur.RoleId == roleId);


            if (existingUserRole is null)
            {
                return Result.Failure(
                    "User does not have this role assigned.");
            }


            userWithRoles.UserRoles.Remove(existingUserRole);

            _unitOfWork.Users.Update(userWithRoles);

            await _unitOfWork.SaveChangesAsync();

            return Result.Success();
        }


        // =========================================================
        // GET PROFILE
        // =========================================================

        public async Task<Result<ProfileDto>> GetProfileAsync(int id)
        {
            var user =
                await _unitOfWork.Users.GetByIdWithRolesAsync(id);

            if (user is null)
            {
                return Result<ProfileDto>.Failure(
                    $"User with ID {id} was not found.");
            }


            var profile = new ProfileDto
            {
                Id = user.Id,

                FullName = user.FullName,

                Email = user.Email,

                UserName = user.UserName,

                CompanyName =
                    user.Company?.Name ?? "N/A",

                Roles = user.UserRoles
                    .Select(ur => ur.Role.Name)
                    .ToList(),

                IsActive = user.IsActive,

                LastLoginAt = user.LastLoginAt
            };


            return Result<ProfileDto>.Success(profile);
        }


        // =========================================================
        // UPDATE PROFILE
        // =========================================================
        // Only FullName will be updated from Profile page.
        // =========================================================

        public async Task<Result> UpdateProfileAsync(
            UpdateProfileDto input)
        {
            var user =
                await _unitOfWork.Users.GetByIdAsync(input.Id);

            if (user is null)
            {
                return Result.Failure(
                    "User not found.");
            }


            // Only FullName is allowed to change
            user.FullName = input.FullName;

            user.UpdatedAt = DateTime.UtcNow;


            // Repository Update() returns void
            _unitOfWork.Users.Update(user);


            // Save changes
            await _unitOfWork.SaveChangesAsync();


            return Result.Success();
        }


        // =========================================================
        // CHANGE PASSWORD
        // =========================================================

        public async Task<Result> ChangePasswordAsync(
            int userId,
            string currentPassword,
            string newPassword)
        {
            var user =
                await _unitOfWork.Users.GetByIdAsync(userId);

            if (user is null)
            {
                return Result.Failure(
                    "User not found.");
            }


            // -----------------------------------------------------
            // Verify current password
            // -----------------------------------------------------

            var passwordValid =
                _passwordHasher.VerifyPassword(
                    user.PasswordHash,
                    currentPassword);


            if (!passwordValid)
            {
                return Result.Failure(
                    "Current password is incorrect.");
            }


            // -----------------------------------------------------
            // Hash new password
            // -----------------------------------------------------

            user.PasswordHash =
                _passwordHasher.HashPassword(
                    newPassword);


            user.UpdatedAt = DateTime.UtcNow;


            // -----------------------------------------------------
            // Update entity
            // -----------------------------------------------------

            _unitOfWork.Users.Update(user);


            // -----------------------------------------------------
            // Save database changes
            // -----------------------------------------------------

            await _unitOfWork.SaveChangesAsync();


            return Result.Success();
        }


        // =========================================================
        // MAP USER TO DTO
        // =========================================================

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

                Roles = user.UserRoles
                    .Select(ur => ur.Role.Name)
                    .ToList()
            };
        }
    }
}