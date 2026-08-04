using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using TaskTrackerDAL.Models.Enums;

namespace TaskTrackerBLL.DTOs.Tasks
{
    public class CreateTaskDto
    {
        [Required(ErrorMessage = "Project is required.")]
        public int ProjectId { get; set; }

        [Required(ErrorMessage = "Task title is required.")]
        [StringLength(200, MinimumLength = 2)]
        public string Title { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }

        public int? AssignedToUserId { get; set; }

        [Required]
        public TaskPriority Priority { get; set; } = TaskPriority.Medium;

        [DataType(DataType.Date)]
        public DateTime? DueDate { get; set; }
    }
}
