using System;
using System.Collections.Generic;
using System.Text;

namespace TaskTrackerBLL.DTOs.Reporting
{
    public class EmployeePerformanceDto
    {
        public int UserId { get; set; }

        public string FullName { get; set; } = string.Empty;

        public int TotalAssignedTasks { get; set; }

        public int CompletedTasks { get; set; }

        public int PendingTasks { get; set; }

        public int OverdueTasks { get; set; }

        public double CompletionRate =>
            TotalAssignedTasks == 0 ? 0 : Math.Round(CompletedTasks * 100.0 / TotalAssignedTasks, 1);
    }
}
