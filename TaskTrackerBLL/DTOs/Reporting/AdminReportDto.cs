using System;
using System.Collections.Generic;
using System.Text;

namespace TaskTrackerBLL.DTOs.Reporting
{
    public class AdminReportDto
    {
        public int TotalCompanies { get; set; }

        public int TotalProjects { get; set; }

        public int TotalTasks { get; set; }

        public int CompletedTasks { get; set; }

        public int PendingTasks { get; set; }

        public int OverdueTasks { get; set; }

        public IReadOnlyList<CompanyPerformanceSummaryDto> CompanyBreakdown { get; set; } =
            Array.Empty<CompanyPerformanceSummaryDto>();
    }
    public class CompanyPerformanceSummaryDto
    {
        public int CompanyId { get; set; }

        public string CompanyName { get; set; } = string.Empty;

        public int TotalProjects { get; set; }

        public int TotalTasks { get; set; }

        public int CompletedTasks { get; set; }

        public double CompletionRate =>
            TotalTasks == 0 ? 0 : Math.Round(CompletedTasks * 100.0 / TotalTasks, 1);
    }
}
