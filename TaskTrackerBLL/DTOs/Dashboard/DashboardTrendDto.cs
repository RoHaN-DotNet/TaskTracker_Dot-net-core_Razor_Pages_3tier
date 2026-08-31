using System;
using System.Collections.Generic;
using System.Text;

namespace TaskTrackerBLL.DTOs.Dashboard
{
    /// <summary>
    /// Time-based growth trends for the admin dashboard line charts.
    /// Returned both as part of <see cref="AdminDashboardDto"/> (for the initial
    /// page load, using the default range) and by the AJAX "change range"
    /// handler (for subsequent range changes without a full page reload).
    /// </summary>
    public class DashboardTrendDto
    {
        public string Range { get; set; } = string.Empty;
        public List<TrendPointDto> CompanyGrowth { get; set; } = new();
        public List<TrendPointDto> UserGrowth { get; set; } = new();
        public List<TrendPointDto> ProjectGrowth {  get; set; } = new();
        public List<TrendPointDto> TaskGrowth { get; set; } = new();
    }
}
