using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace TaskTrackerBLL.DTOs.Auth
{
    public class ChangePasswordDto
    {
        public class ChangePasswordInput
        {
            [Required]
            [DataType(DataType.Password)]
            public string CurrentPassword { get; set; } = string.Empty;

            [Required]
            [DataType(DataType.Password)]
            public string NewPassword { get; set; } = string.Empty;

            [Required]
            [DataType(DataType.Password)]
            [Compare(nameof(NewPassword), ErrorMessage = "New password and confirm password do not match.")]
            public string ConfirmPassword { get; set; } = string.Empty;
        }
    }
}
