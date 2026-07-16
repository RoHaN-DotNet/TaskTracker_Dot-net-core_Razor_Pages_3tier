using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Text;
using TaskTrackerBLL.Common;
using TaskTrackerBLL.DTOs.Employee;
using TaskTrackerBLL.Interfaces;
using TaskTrackerBLL.Interfaces.Services;
using TaskTrackerBLL.Interfaces.Security;
using TaskTrackerDAL.Constants;
using TaskTrackerDAL.Models;

namespace TaskTrackerBLL.Services
{
    public class EmployeeService:IEmployeeService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPasswordHasher _passwordHasher;

        public EmployeeService(IUnitOfWork unitOfWork, IPasswordHasher passwordHasher)
        {
            _unitOfWork = unitOfWork;
            _passwordHasher = passwordHasher;
        }

        public async Task<Result<EmployeeDto>> GetByIdAsync(int id, int? actingManagerCompanyId)
        {
            var employee = await _unitOfWork.Users.GetByIdWithRolesAsync(id);

            if (employee is null || !IsEmployee(employee))
            {
                return Result<EmployeeDto>.Failure($"Employee with ID {id} was not found.");
            }

            if (actingManagerCompanyId.HasValue && employee.CompanyId != actingManagerCompanyId.Value)
            {
                return Result<EmployeeDto>.Failure("You are not authorized to view this employee.");
            }

            var dto = await MapToDtoAsync(employee);

            return Result<EmployeeDto>.Success(dto);
        }

        public async Task<Result<IReadOnlyList<EmployeeDto>>> SearchAsync(
            EmployeeSearchFilterDto filter,
            int? actingManagerCompanyId)
        {
            var employees = await _unitOfWork.Users.SearchEmployeesAsync(
                companyId: actingManagerCompanyId,
                searchTerm: filter.SearchTerm,
                roleName: filter.RoleName,
                isActive: filter.IsActive);

            var dtos = new List<EmployeeDto>();
            foreach (var employee in employees)
            {
                dtos.Add(await MapToDtoAsync(employee));
            }

            return Result<IReadOnlyList<EmployeeDto>>.Success(dtos);
        }

        public async Task<Result<EmployeeDto>> RegisterAsync(SignupEmployeeDto dto, int actingUserCompanyId)
        {
            if (!AppRoles.EmployeeRoles.Contains(dto.RoleName))
            {
                return Result<EmployeeDto>.Failure(
                    $"'{dto.RoleName}' is not a valid employee role.");
            }

            var company = await _unitOfWork.Companies.GetByIdAsync(actingUserCompanyId);
            if (company is null)
            {
                return Result<EmployeeDto>.Failure("Your company could not be found.");
            }

            var isEmailUnique = await _unitOfWork.Users.IsEmailUniqueAsync(dto.Email);
            if (!isEmailUnique)
            {
                return Result<EmployeeDto>.Failure($"Email '{dto.Email}' is already registered.");
            }

            var isUserNameUnique = await _unitOfWork.Users.IsUserNameUniqueAsync(dto.UserName);
            if (!isUserNameUnique)
            {
                return Result<EmployeeDto>.Failure($"Username '{dto.UserName}' is already taken.");
            }

            var role = await _unitOfWork.Roles.GetByNameAsync(dto.RoleName);
            if (role is null)
            {
                return Result<EmployeeDto>.Failure(
                    $"Role '{dto.RoleName}' has not been configured. Contact an administrator.");
            }

            var employee = new User
            {
                CompanyId = actingUserCompanyId,
                FullName = dto.FullName,
                Email = dto.Email,
                UserName = dto.UserName,
                PasswordHash = _passwordHasher.HashPassword(dto.Password),
                IsActive = true
            };

            employee.UserRoles.Add(new UserRole
            {
                RoleId = role.Id
            });

            await _unitOfWork.Users.AddAsync(employee);
            await _unitOfWork.SaveChangesAsync();

            var dtoResult = await MapToDtoAsync(employee);

            return Result<EmployeeDto>.Success(dtoResult);
        }

        public async Task<Result> UpdateAsync(EditEmployeeDto dto, int? actingManagerCompanyId)
        {
            var employee = await _unitOfWork.Users.GetByIdWithRolesAsync(dto.Id);

            if (employee is null || !IsEmployee(employee))
            {
                return Result.Failure($"Employee with ID {dto.Id} was not found.");
            }

            if (actingManagerCompanyId.HasValue && employee.CompanyId != actingManagerCompanyId.Value)
            {
                return Result.Failure("You are not authorized to edit this employee.");
            }

            if (!AppRoles.EmployeeRoles.Contains(dto.RoleName))
            {
                return Result.Failure($"'{dto.RoleName}' is not a valid employee role.");
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

            var newRole = await _unitOfWork.Roles.GetByNameAsync(dto.RoleName);
            if (newRole is null)
            {
                return Result.Failure($"Role '{dto.RoleName}' has not been configured.");
            }

            employee.FullName = dto.FullName;
            employee.Email = dto.Email;
            employee.UserName = dto.UserName;
            employee.UpdatedAt = DateTime.UtcNow;

            var currentRoleLink = employee.UserRoles.FirstOrDefault(ur =>
                AppRoles.EmployeeRoles.Contains(ur.Role.Name));

            if (currentRoleLink is not null && currentRoleLink.RoleId != newRole.Id)
            {
                employee.UserRoles.Remove(currentRoleLink);
                employee.UserRoles.Add(new UserRole { UserId = employee.Id, RoleId = newRole.Id });
            }
            else if (currentRoleLink is null)
            {
                employee.UserRoles.Add(new UserRole { UserId = employee.Id, RoleId = newRole.Id });
            }

            _unitOfWork.Users.Update(employee);
            await _unitOfWork.SaveChangesAsync();

            return Result.Success();
        }

        public async Task<Result> DisableAsync(int id, int? actingManagerCompanyId)
        {
            var employee = await _unitOfWork.Users.GetByIdWithRolesAsync(id);

            if (employee is null || !IsEmployee(employee))
            {
                return Result.Failure($"Employee with ID {id} was not found.");
            }

            if (actingManagerCompanyId.HasValue && employee.CompanyId != actingManagerCompanyId.Value)
            {
                return Result.Failure("You are not authorized to disable this employee.");
            }

            if (!employee.IsActive)
            {
                return Result.Failure($"{employee.FullName} is already disabled.");
            }

            employee.IsActive = false;
            employee.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Users.Update(employee);
            await _unitOfWork.SaveChangesAsync();

            return Result.Success();
        }

        private static bool IsEmployee(User user)
        {
            return user.UserRoles.Any(ur => AppRoles.EmployeeRoles.Contains(ur.Role.Name));
        }

        private async Task<EmployeeDto> MapToDtoAsync(User employee)
        {
            var company = await _unitOfWork.Companies.GetByIdAsync(employee.CompanyId);

            return new EmployeeDto
            {
                Id = employee.Id,
                CompanyId = employee.CompanyId,
                CompanyName = company?.Name ?? "Unknown",
                FullName = employee.FullName,
                Email = employee.Email,
                UserName = employee.UserName,
                IsActive = employee.IsActive,
                LastLoginAt = employee.LastLoginAt,
                Roles = employee.UserRoles.Select(ur => ur.Role.Name).ToList(),
                CreatedAt = employee.CreatedAt
            };
        }
    }
}
