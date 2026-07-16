using System;
using System.Collections.Generic;
using System.Text;

namespace TaskTrackerBLL.DTOs.User
{
    public class UserDto
    {
        public int Id { get; set; }

        public int CompanyId { get; set; }

        public string FullName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string UserName { get; set; } = string.Empty;

        public bool IsActive { get; set; }

        public DateTime? LastLoginAt { get; set; }

        public IReadOnlyList<string> Roles { get; set; } = Array.Empty<string>();
    }
}
