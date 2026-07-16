using System;
using System.Collections.Generic;
using System.Text;
using TaskTrackerBLL.Common;
using TaskTrackerBLL.DTOs.Dashboard;

namespace TaskTrackerBLL.Interfaces.Services
{
    public interface IDashboardService
    {
        Task<Result<DashboardDto>> GetDashboardAsync(int companyId);
    }
}
