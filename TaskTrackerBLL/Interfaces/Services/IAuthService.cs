using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;
using TaskTrackerBLL.Common;
using TaskTrackerBLL.DTOs.Auth;

namespace TaskTrackerBLL.Interfaces.Services
{
    public interface IAuthService
    {
        Task<Result> SignupAsync(SignupDto dto);

        Task<Result<ClaimsPrincipal>> LoginAsync(LoginDto dto);
    }
}
