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

        // =====================================================
        // GROWTH TRENDS (time-based line charts)
        // Populated using the default range ("12m") on initial load.
        // The dropdown on the page re-fetches these via the
        // OnGetTrendsAsync AJAX handler when the user changes the range.
        // =====================================================

        public string TrendRange { get; set; } = "12m";

        public List<TrendPointDto> CompanyGrowth { get; set; } = new();

        public List<TrendPointDto> UserGrowth { get; set; } = new();

        public List<TrendPointDto> ProjectGrowth { get; set; } = new();

        public List<TrendPointDto> TaskGrowth { get; set; } = new();

        // =====================================================
        // BREAKDOWN CHARTS (not time-based, always "current state")
        // =====================================================

        public List<TrendPointDto> UsersByRole { get; set; } = new();

        public List<TrendPointDto> ProjectsByStatus { get; set; } = new();

        public List<TrendPointDto> TasksByStatus { get; set; } = new();
    }
}
