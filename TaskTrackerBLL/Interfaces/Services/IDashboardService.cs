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

        /// <summary>
        /// Returns Company/User/Project/Task growth trends for the given range.
        /// Valid range values: "7d", "30d", "6m", "12m" (defaults to "12m" for anything else).
        /// </summary>
        Task<Result<DashboardTrendDto>> GetAdminDashboardTrendsAsync(string range);

        Task<Result<ManagerDashboardDto>> GetManagerDashboardAsync(int companyId);
        Task<Result<EmployeeDashboardDto>> GetEmployeeDashboardAsync(int userId);
    }
}
