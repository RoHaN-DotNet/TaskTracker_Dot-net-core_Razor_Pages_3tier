using System;
using System.Collections.Generic;
using System.Text;

namespace TaskTrackerBLL.DTOs.Project
{
    public class ProjectMemberDto
    {
        public int UserId { get; set; }

        public string FullName { get; set; } = string.Empty;

        public List<string> Roles { get; set; } = new();
    }
}
