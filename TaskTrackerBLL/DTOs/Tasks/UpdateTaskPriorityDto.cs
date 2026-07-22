using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using TaskTrackerDAL.Models.Enums;

namespace TaskTrackerBLL.DTOs.Tasks
{
    public class UpdateTaskPriorityDto
    {
        [Required]
        public int TaskId { get; set; }

        [Required]
        public ProjectTasksStatus NewStatus { get; set; }
    }
}
