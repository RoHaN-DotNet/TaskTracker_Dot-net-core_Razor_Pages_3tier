using System;
using System.Collections.Generic;
using System.Text;
using TaskTrackerBLL.DTOs.Company;
using TaskTrackerBLL.DTOs.Employee;
using TaskTrackerBLL.DTOs.Project;
using TaskTrackerBLL.DTOs.Task;

namespace TaskTrackerBLL.DTOs.Dashboard
{
    public class ManagerDashboardDto
    {
        public CompanyDto MyCompany { get; set; } = default!;
        public IReadOnlyList<EmployeeDto> MyEmployees { get; set; }=Array.Empty<EmployeeDto>();
        public IReadOnlyList<ProjectDto> MyProjects { get; set; } = Array.Empty<ProjectDto>();
        public IReadOnlyList<TaskDto> TodaysDeadline { get; set; } =Array.Empty<TaskDto>();
        public IReadOnlyList<TaskDto> RecentTasks { get; set; } = Array.Empty<TaskDto>();
    }
}
