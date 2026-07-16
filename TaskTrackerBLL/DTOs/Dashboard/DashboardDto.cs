using System;
using System.Collections.Generic;
using System.Text;

namespace TaskTrackerBLL.DTOs.Dashboard
{
    public class DashboardDto
    {
        public int TotalProjects { get; set; }

        public int ActiveProjects { get; set; }

        public int CompletedProjects { get; set; }

        public int TotalOpenTasks { get; set; }

        public int TotalCompletedTasks { get; set; }

        public int OverdueTaskCount { get; set; }

        public Dictionary<string, int> OpenTaskCountsByUser { get; set; } = new();
    }
}
