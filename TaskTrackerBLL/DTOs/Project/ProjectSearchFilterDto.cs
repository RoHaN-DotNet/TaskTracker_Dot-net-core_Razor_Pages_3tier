using System;
using System.Collections.Generic;
using System.Text;
using TaskTrackerDAL.Models.Enums;

namespace TaskTrackerBLL.DTOs.Project
{
    public class ProjectSearchFilterDto
    {
        public string? SearchTerm { get; set; }

        public ProjectStatus? Status { get; set; }

        public int PageNumber { get; set; } = 1;

        public int PageSize { get; set; } = 10;
    }
}
