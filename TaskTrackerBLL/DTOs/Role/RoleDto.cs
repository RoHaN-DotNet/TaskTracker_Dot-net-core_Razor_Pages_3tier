using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace TaskTrackerBLL.DTOs.Role
{
    public class RoleDto
    {
        public int Id { get; set; }
        
        
        public string Name { get; set; } = string.Empty;
        
        public string? Description { get; set; }

        public int TotalUsersAssigned { get; set; }
    }
}
