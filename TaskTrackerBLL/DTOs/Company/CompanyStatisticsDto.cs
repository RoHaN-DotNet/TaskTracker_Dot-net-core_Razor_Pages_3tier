using TaskTrackerDAL.Models.Enums;

namespace TaskTrackerBLL.DTOs.Company
{
    public class CompanyStatisticsDto
    {
        
        public int CompanyId { get; set; }

        public string CompanyName { get; set; } = string.Empty;

        public int TotalUsers { get; set; }

        public int ActiveUsers { get; set; }

        public int TotalProjects { get; set; }

        public Dictionary<ProjectStatus, int> ProjectsByStatus { get; set; } = new();

        public int TotalTasks { get; set; }

        public int CompletedTasks { get; set; }

        public int OverdueTasks { get; set; }
    }
}
