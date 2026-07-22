using System;
using System.Collections.Generic;
using System.Text;

namespace TaskTrackerBLL.DTOs.Dashboard
{
    public class AdminDashboardDto
    {
        public int TotalCompanies { get; set; }

        public int TotalUsers { get; set; }

        public int TotalManagers { get; set; }

        public int TotalEmployees { get; set; }

        public int TotalProjects { get; set; }

        public int TotalTasks { get; set; }

        public int CompletedTasks { get; set; }

        public int PendingTasks { get; set; }

        public int OverdueTasks { get; set; }
    }
}
