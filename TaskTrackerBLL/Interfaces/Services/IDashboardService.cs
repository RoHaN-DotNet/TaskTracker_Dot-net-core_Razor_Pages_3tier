using System;
using System.Collections.Generic;
using System.Text;
using TaskTrackerBLL.Common;
using TaskTrackerBLL.DTOs.Dashboard;

namespace TaskTrackerBLL.Interfaces.Services
{
    public interface IDashboardService
    {
        Task<Result<AdminDashboardDto>> GetAdminDashboardAsync();

        Task<Result<ManagerDashboardDto>> GetManagerDashboardAsync(int companyId);
        Task<Result<EmployeeDashboardDto>> GetEmployeeDashboardAsync(int userId);
    }
}
