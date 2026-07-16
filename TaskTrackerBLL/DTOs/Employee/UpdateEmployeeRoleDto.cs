using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace TaskTrackerBLL.DTOs.Employee
{
    public class UpdateEmployeeRoleDto
    {
        [Required]
        public int EmployeeId { get; set; }

        [Required(ErrorMessage = "Please select a role.")]
        public string NewRoleName { get; set; } = string.Empty;
    }
}
