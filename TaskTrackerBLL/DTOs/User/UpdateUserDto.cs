using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace TaskTrackerBLL.DTOs.User
{
    public class UpdateUserDto
    {
        [Required]
        public int Id { get; set; }

        [Required(ErrorMessage = "Full name is required.")]
        [StringLength(200, MinimumLength = 2)]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress]
        [StringLength(200)]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Username is required.")]
        [StringLength(100, MinimumLength = 3)]
        public string UserName { get; set; } = string.Empty;

        public bool IsActive { get; set; }

    }
}
