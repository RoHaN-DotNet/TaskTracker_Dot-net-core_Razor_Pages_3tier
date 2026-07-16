using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace TaskTrackerBLL.DTOs.Project
{
    public class CreateProjectDto
    {
        [Required(ErrorMessage = "Company is required.")]
        public int CompanyId { get; set; }

        [Required(ErrorMessage = "Project name is required.")]
        [StringLength(200, MinimumLength = 2)]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Start date is required.")]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; } = DateTime.UtcNow.Date;

        [DataType(DataType.Date)]
        public DateTime? EndDate { get; set; }

        public List<int> InitialMemberUserIds { get; set; } = new();
    }
}
