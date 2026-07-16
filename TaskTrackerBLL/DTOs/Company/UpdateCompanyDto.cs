using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace TaskTrackerBLL.DTOs.Company
{
    public class UpdateCompanyDto
    {
        [Required]
        public int Id { get; set; }

        [Required(ErrorMessage = "Company name is required.")]
        [StringLength(200, MinimumLength = 2)]
        public string Name { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        [StringLength(200)]
        public string? Email { get; set; }

        [Phone(ErrorMessage = "Enter a valid phone number.")]
        [StringLength(50)]
        public string? Phone { get; set; }

        [StringLength(500)]
        public string? Address { get; set; }

        public bool IsActive { get; set; }
    }
}
