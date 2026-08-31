using System;
using System.Collections.Generic;
using System.Text;
using TaskTrackerBLL.Common;
using TaskTrackerBLL.DTOs.Employee;

namespace TaskTrackerBLL.Interfaces.Services
{
    public interface IEmployeeService
    {
        Task<Result<EmployeeDto>> GetByIdAsync(int id, int? actingManagerCompanyId);
        Task<Result<IReadOnlyList<EmployeeDto>>> GetAllAsync(int? companyId);
        Task<Result<IReadOnlyList<EmployeeDto>>> SearchAsync(
            EmployeeSearchFilterDto filter,int? actingManagerCompanyId, int? actingManagerUserId = null);

        Task<Result<EmployeeDto>> RegisterAsync(SignupEmployeeDto dto, int? actingUserCompanyId);

        Task<Result> UpdateAsync(EditEmployeeDto dto, int? actingManagerCompanyId);

        Task<Result> DisableAsync(int id,bool isActive);
    }
}
