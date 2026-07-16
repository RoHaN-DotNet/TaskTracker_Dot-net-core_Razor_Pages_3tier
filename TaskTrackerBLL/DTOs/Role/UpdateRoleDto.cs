using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace TaskTrackerBLL.DTOs.Role
{
    public class UpdateRoleDto
    {
        [Required]
        public int Id { get; set; }

        [Required(ErrorMessage = "Role name is required.")]
        [StringLength(100, MinimumLength = 2)]
        public string Name { get; set; } = string.Empty;

        [StringLength(300)]
        public string? Description { get; set; }
    }
}
