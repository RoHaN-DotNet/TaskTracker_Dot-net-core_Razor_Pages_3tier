using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using TaskTrackerBLL.Common;
using TaskTrackerBLL.DTOs.Employee;
using TaskTrackerBLL.Interfaces;
using TaskTrackerBLL.Interfaces.Security;
using TaskTrackerBLL.Interfaces.Services;
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
        public async Task<Result<IReadOnlyList<EmployeeDto>>> GetAllAsync(int? companyId)
        {
            IReadOnlyList<User> employees;

            if (companyId.HasValue)
                employees = await _unitOfWork.Users.GetByCompanyIdAsync(companyId.Value);
            else
                employees = await _unitOfWork.Users.GetAllAsync();

            var dtos = new List<EmployeeDto>();

            foreach (var employee in employees)
            {
                dtos.Add(await MapToDtoAsync(employee));
            }

            return Result<IReadOnlyList<EmployeeDto>>.Success(dtos);
        }
        public async Task<Result<IReadOnlyList<EmployeeDto>>> SearchAsync(
    EmployeeSearchFilterDto filter,
    int? actingManagerCompanyId,
    int? actingManagerUserId = null)
        {
            var employees = await _unitOfWork.Users.SearchEmployeesAsync(
                companyId: actingManagerCompanyId,
                searchTerm: filter.SearchTerm,
                roleName: filter.RoleName,
                isActive: filter.IsActive);

            var dtos = new List<EmployeeDto>();

            foreach (var employee in employees)
            {
                var dto = await MapToDtoAsync(employee);

                // ============================================
                // MANAGER CANNOT SEE:
                // 1. Himself
                // 2. Other Managers
                // ============================================

                if (actingManagerUserId.HasValue)
                {
                    // Hide himself
                    if (dto.Id == actingManagerUserId.Value)
                    {
                        continue;
                    }

                    // Hide all Managers
                    if (dto.Roles != null &&
                        dto.Roles.Any(r =>
                            r.Equals(AppRoles.Manager,
                                StringComparison.OrdinalIgnoreCase)))
                    {
                        continue;
                    }
                }

                dtos.Add(dto);
            }

            return Result<IReadOnlyList<EmployeeDto>>.Success(dtos);
        }

        public async Task<Result<EmployeeDto>> RegisterAsync(
    SignupEmployeeDto dto,
    int? actingUserCompanyId)
        {
            // -------------------------------------------------
            // 1. Validate Role
            // -------------------------------------------------
            if (!AppRoles.EmployeeRoles.Contains(dto.RoleName))
            {
                return Result<EmployeeDto>.Failure(
                    $"'{dto.RoleName}' is not a valid employee role.");
            }

            // -------------------------------------------------
            // 2. Determine Company
            // -------------------------------------------------
            int companyId;

            if (actingUserCompanyId.HasValue)
            {
                // MANAGER
                // Manager's company is ALWAYS used.
                // dto.CompanyId is completely ignored.
                companyId = actingUserCompanyId.Value;
            }
            else
            {
                // ADMIN
                // Admin selects the company from dropdown.
                companyId = dto.CompanyId;
            }

            // -------------------------------------------------
            // 3. Check Company exists
            // -------------------------------------------------
            var company = await _unitOfWork.Companies.GetByIdAsync(companyId);

            if (company is null)
            {
                return Result<EmployeeDto>.Failure(
                    "The selected company could not be found.");
            }

            // -------------------------------------------------
            // 4. Check Email
            // -------------------------------------------------
            var isEmailUnique =
                await _unitOfWork.Users.IsEmailUniqueAsync(dto.Email);

            if (!isEmailUnique)
            {
                return Result<EmployeeDto>.Failure(
                    $"Email '{dto.Email}' is already registered.");
            }

            // -------------------------------------------------
            // 5. Check Username
            // -------------------------------------------------
            var isUserNameUnique =
                await _unitOfWork.Users.IsUserNameUniqueAsync(dto.UserName);

            if (!isUserNameUnique)
            {
                return Result<EmployeeDto>.Failure(
                    $"Username '{dto.UserName}' is already taken.");
            }

            // -------------------------------------------------
            // 6. Get Role
            // -------------------------------------------------
            var role =
                await _unitOfWork.Roles.GetByNameAsync(dto.RoleName);

            if (role is null)
            {
                return Result<EmployeeDto>.Failure(
                    $"Role '{dto.RoleName}' has not been configured. Contact an administrator.");
            }

            // -------------------------------------------------
            // 7. Create Employee
            // -------------------------------------------------
            var employee = new User
            {
                // IMPORTANT:
                // Manager -> his own company
                // Admin   -> selected company
                CompanyId = companyId,

                FullName = dto.FullName,

                Email = dto.Email,

                UserName = dto.FullName
                           .Trim()
                           .ToLower(),

                PasswordHash =
                    _passwordHasher.HashPassword(dto.Password),

                IsActive = dto.isActive
            };

            // -------------------------------------------------
            // 8. Assign Role
            // -------------------------------------------------
            employee.UserRoles.Add(new UserRole
            {
                RoleId = role.Id
            });

            // -------------------------------------------------
            // 9. Save
            // -------------------------------------------------
            await _unitOfWork.Users.AddAsync(employee);

            await _unitOfWork.SaveChangesAsync();

            // -------------------------------------------------
            // 10. Get saved employee with roles
            // -------------------------------------------------
            var savedEmployee =
                await _unitOfWork.Users.GetByIdWithRolesAsync(employee.Id);

            var dtoResult =
                await MapToDtoAsync(savedEmployee);

            return Result<EmployeeDto>.Success(dtoResult);
        }

        public async Task<Result> UpdateAsync(
    EditEmployeeDto dto,
    int? actingManagerCompanyId)
        {
            // =========================================================
            // Get employee with roles
            //
            // GetByIdWithRolesAsync() uses AsNoTracking()
            // so this employee object will NOT be tracked.
            // =========================================================

            var employee =
                await _unitOfWork.Users.GetByIdWithRolesAsync(dto.Id);

            if (employee is null ||!IsEmployee(employee))
            {
                return Result.Failure(
                    $"Employee with ID {dto.Id} was not found.");
            }


            // =========================================================
            // Validate role
            // =========================================================

            if (string.IsNullOrWhiteSpace(dto.RoleName))
            {
                return Result.Failure(
                    "Please select a role.");
            }

            var roleName = dto.RoleName.Trim();

            if (!AppRoles.EmployeeRoles.Contains(roleName))
            {
                return Result.Failure(
                    $"'{roleName}' is not a valid employee role.");
            }


            // =========================================================
            // Email uniqueness
            // =========================================================

            var isEmailUnique =
                await _unitOfWork.Users.IsEmailUniqueAsync(
                    dto.Email,
                    dto.Id);

            if (!isEmailUnique)
            {
                return Result.Failure(
                    $"Email '{dto.Email}' is already in use.");
            }


            // =========================================================
            // Username uniqueness
            // =========================================================

            var isUserNameUnique =
                await _unitOfWork.Users.IsUserNameUniqueAsync(
                    dto.UserName,
                    dto.Id);

            if (!isUserNameUnique)
            {
                return Result.Failure(
                    $"Username '{dto.UserName}' is already in use.");
            }


            // =========================================================
            // Get selected role
            // =========================================================

            var newRole =
                await _unitOfWork.Roles.GetByNameAsync(roleName);

            if (newRole is null)
            {
                return Result.Failure(
                    $"Role '{roleName}' has not been configured.");
            }


            // =========================================================
            // Get TRACKED User
            //
            // We do NOT use the AsNoTracking employee object
            // for User update.
            //
            // This prevents:
            //
            // "another instance with the same key value"
            //
            // =========================================================

            var userToUpdate =
                await _unitOfWork.Users.GetByIdAsync(dto.Id);

            if (userToUpdate is null)
            {
                return Result.Failure(
                    $"Employee with ID {dto.Id} was not found.");
            }


            // =========================================================
            // Update User table
            // =========================================================

            userToUpdate.FullName = dto.FullName;
            userToUpdate.Email = dto.Email;
            userToUpdate.UserName = dto.UserName;
            userToUpdate.UpdatedAt = DateTime.UtcNow;


            // =========================================================
            // Get current employee roles
            //
            // This comes from the AsNoTracking employee object.
            // We only use it to know which UserRole rows exist.
            // =========================================================

            var currentEmployeeRoles =
                employee.UserRoles
                    .Where(ur =>
                        ur.Role != null &&
                        AppRoles.EmployeeRoles.Contains(
                            ur.Role.Name))
                    .ToList();


            // =========================================================
            // Check whether selected role already exists
            //
            // Uses the UserRole repository method you created.
            // =========================================================

            var existingNewRole =
                await _unitOfWork.UserRoles.GetAsync(
                    dto.Id,
                    newRole.Id);


            // =========================================================
            // Remove all OLD employee roles
            //
            // User should have ONLY ONE employee role.
            // =========================================================

            foreach (var oldRole in currentEmployeeRoles)
            {
                if (oldRole.RoleId != newRole.Id)
                {
                    await _unitOfWork.UserRoles.DeleteAsync(
                        oldRole.UserId,
                        oldRole.RoleId);
                }
            }


            // =========================================================
            // Add selected role if it doesn't already exist
            // =========================================================

            if (existingNewRole is null)
            {
                await _unitOfWork.UserRoles.AddAsync(
                    new UserRole
                    {
                        UserId = dto.Id,
                        RoleId = newRole.Id
                    });
            }


            // =========================================================
            // Save everything
            // =========================================================

            await _unitOfWork.SaveChangesAsync();


            return Result.Success();
        }

        public async Task<Result> DisableAsync(int id,bool isActive)
        {
            var employee = await _unitOfWork.Users.GetByIdWithRolesAsync(id);
            
            
            if (employee is null)
            {
                return Result.Failure($"Employee with ID {id} was not found.");
            }
           
            /*
    if (actingManagerCompanyId.HasValue && employee.CompanyId != actingManagerCompanyId.Value)
    {
        return Result.Failure("You are not authorized to disable this employee.");
    }

    if (!employee.IsActive)
    {
        return Result.Failure($"{employee.FullName} is already disabled.");
    }*/

            employee.IsActive = isActive;
            employee.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Users.Update(employee);
            await _unitOfWork.SaveChangesAsync();

            return Result.Success();
        }

        private static bool IsEmployee(User user)
        {
            return user.UserRoles.Any(ur => AppRoles.EmployeeRoles.Contains(ur.Role.Name));
        }
        private static bool IsManager(User user)
        {
            return user.UserRoles.Any(ur => AppRoles.Manager.Contains(ur.Role.Name));
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
