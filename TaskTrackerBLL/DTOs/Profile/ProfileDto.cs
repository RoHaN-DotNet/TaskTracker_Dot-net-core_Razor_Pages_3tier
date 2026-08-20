using System;
using System.Collections.Generic;
using System.Text;

namespace TaskTrackerBLL.DTOs.Profile
{
    public class ProfileDto
    {
        public int Id { get; set; }

        public string FullName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string UserName { get; set; } = string.Empty;

        public string CompanyName { get; set; } = string.Empty;

        public List<string> Roles { get; set; } = new();

        public bool IsActive { get; set; }

        public DateTime? LastLoginAt { get; set; }
    }
}
