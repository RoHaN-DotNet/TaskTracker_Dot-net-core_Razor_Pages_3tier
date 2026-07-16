using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;
using TaskTrackerBLL.Common;
using TaskTrackerBLL.DTOs.Auth;
using TaskTrackerBLL.Interfaces;
using TaskTrackerBLL.Interfaces.Services;
using TaskTrackerBLL.Interfaces.Security;
using TaskTrackerDAL.Models;

namespace TaskTrackerBLL.Services
{
    public class AuthService:IAuthService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPasswordHasher _passwordHasher;

        public AuthService(IUnitOfWork unitOfWork, IPasswordHasher passwordHasher)
        {
            _unitOfWork = unitOfWork;
            _passwordHasher = passwordHasher;
        }

        public async Task<Result> SignupAsync(SignupDto dto)
        {
            var company = await _unitOfWork.Companies.GetByIdAsync(dto.CompanyId);
            if (company is null)
            {
                return Result.Failure($"Company with ID {dto.CompanyId} was not found.");
            }

            var isEmailUnique = await _unitOfWork.Users.IsEmailUniqueAsync(dto.Email);
            if (!isEmailUnique)
            {
                return Result.Failure($"Email '{dto.Email}' is already registered.");
            }

            var isUserNameUnique = await _unitOfWork.Users.IsUserNameUniqueAsync(dto.UserName);
            if (!isUserNameUnique)
            {
                return Result.Failure($"Username '{dto.UserName}' is already taken.");
            }

            var user = new User
            {
                CompanyId = dto.CompanyId,
                FullName = dto.FullName,
                Email = dto.Email,
                UserName = dto.UserName,
                PasswordHash = _passwordHasher.HashPassword(dto.Password),
                IsActive = true
            };

            await _unitOfWork.Users.AddAsync(user);
            await _unitOfWork.SaveChangesAsync();

            return Result.Success();
        }

        public async Task<Result<ClaimsPrincipal>> LoginAsync(LoginDto dto)
        {
            var user = await _unitOfWork.Users.GetByEmailAsync(dto.Email);

            if (user is null || !user.IsActive)
            {
                return Result<ClaimsPrincipal>.Failure("Invalid email or password.");
            }

            var passwordValid = _passwordHasher.VerifyPassword(user.PasswordHash, dto.Password);

            if (!passwordValid)
            {
                return Result<ClaimsPrincipal>.Failure("Invalid email or password.");
            }

            var userWithRoles = await _unitOfWork.Users.GetByIdWithRolesAsync(user.Id);

            var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.UserName),
            new(ClaimTypes.Email, user.Email),
            new("CompanyId", user.CompanyId.ToString())
        };

            foreach (var roleName in userWithRoles!.UserRoles.Select(ur => ur.Role.Name))
            {
                claims.Add(new Claim(ClaimTypes.Role, roleName));
            }

            var identity = new ClaimsIdentity(claims, authenticationType: "CookieAuth");
            var principal = new ClaimsPrincipal(identity);

            user.LastLoginAt = DateTime.UtcNow;
            _unitOfWork.Users.Update(user);
            await _unitOfWork.SaveChangesAsync();

            return Result<ClaimsPrincipal>.Success(principal);
        }
    }
}
