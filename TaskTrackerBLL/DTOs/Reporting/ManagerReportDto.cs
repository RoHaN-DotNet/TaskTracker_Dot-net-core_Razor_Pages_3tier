using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
using System;
using System.Collections.Generic;
using System.Text;
using TaskTrackerBLL.DTOs.Task;
using TaskTrackerBLL.DTOs.Tasks;

namespace TaskTrackerBLL.DTOs.Reporting
{
    public class ManagerReportDto
    {
        public IReadOnlyList<EmployeePerformanceDto> EmployeePerformance { get; set; } = Array.Empty<EmployeePerformanceDto>();

        public IReadOnlyList<ProjectProgressDto> ProjectProgress { get; set; } = Array.Empty<ProjectProgressDto>();

        public IReadOnlyList<TaskDto> CompletedTasks { get; set; } = Array.Empty<TaskDto>();
        public IReadOnlyList<TaskDto> PendingTasks { get; set; }=Array.Empty<TaskDto>();

        public IReadOnlyList<TaskDto> OverDueTasks { get; set; } = Array.Empty<TaskDto>();

    }
}
