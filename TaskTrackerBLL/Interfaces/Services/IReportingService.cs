using System;
using System.Collections.Generic;
using System.Text;
using TaskTrackerBLL.Common;
using TaskTrackerBLL.DTOs.Reporting;

namespace TaskTrackerBLL.Interfaces.Services
{
    public interface IReportingService
    {
        Task<Result<ManagerReportDto>> GetManagerReportAsync(int companyId);

        Task<Result<AdminReportDto>> GetAdminReportAsync();
    }
}
