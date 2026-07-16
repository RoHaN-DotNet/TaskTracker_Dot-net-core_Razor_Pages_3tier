using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using TaskTrackerDAL.Models.Enums;

namespace TaskTrackerBLL.DTOs.Project
{
    public class UpdateProjectDto
    {
        [Required]
        public int Id { get; set; }

        [Required(ErrorMessage = "Project name is required.")]
        [StringLength(200, MinimumLength = 2)]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Start date is required.")]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime? EndDate { get; set; }

        [Required]
        public ProjectStatus Status { get; set; }
    }
}
