using System;
using System.Collections.Generic;
using System.Text;

namespace TaskTrackerBLL.DTOs.Employee
{
    public class EmployeeSearchFilterDto
    {
        public string? SearchTerm { get; set; }

        public string? RoleName { get; set; }

        public bool? IsActive { get; set; }
    }
}
