using System;
using System.Collections.Generic;
using System.Text;

namespace TaskTrackerBLL.DTOs.Company
{
    public class CompanyDto
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? Email { get; set; }

        public string? Phone { get; set; }

        public string? Address { get; set; }

        public bool IsActive { get; set; }

        public int TotalUsers { get; set; }

        public int TotalProjects { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
