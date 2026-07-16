using System;
using System.Collections.Generic;
using System.Text;
using TaskTrackerDAL.Models.Enums;

namespace TaskTrackerBLL.DTOs.Project
{
    public class ProjectDto
    {
        public int Id { get; set; }

        public int CompanyId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public ProjectStatus Status { get; set; }

        public string CreatedByUserName { get; set; } = string.Empty;

        public int TotalTasks { get; set; }

        public int CompletedTasks { get; set; }

        public int TeamMemberCount { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
