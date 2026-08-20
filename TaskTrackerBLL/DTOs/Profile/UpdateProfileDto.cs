using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace TaskTrackerBLL.DTOs.Profile
{
    public class UpdateProfileDto
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Full name is required.")]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;
    }
}
