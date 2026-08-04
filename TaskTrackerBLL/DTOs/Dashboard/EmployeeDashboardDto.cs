using System;
using System.Collections.Generic;
using System.Text;
using TaskTrackerBLL.DTOs.Project;
using TaskTrackerBLL.DTOs.Tasks;

namespace TaskTrackerBLL.DTOs.Dashboard
{
    public class EmployeeDashboardDto
    {
        public IReadOnlyList<ProjectDto> AssignedProjects { get; set; } = Array.Empty<ProjectDto>();

        public IReadOnlyList<TaskDto> AssignedTasks { get; set; } = Array.Empty<TaskDto>();
        public IReadOnlyList<TaskDto> CompletedTasks { get; set; } = Array.Empty<TaskDto>();
        public IReadOnlyList<TaskDto> PendingTasks { get; set; } = Array.Empty<TaskDto>();
        public IReadOnlyList<TaskDto> TodaysDeadlines { get; set; }= Array.Empty<TaskDto>();
    }
}
