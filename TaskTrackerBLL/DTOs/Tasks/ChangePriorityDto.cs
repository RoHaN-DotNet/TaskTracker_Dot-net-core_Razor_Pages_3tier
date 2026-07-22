using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using TaskTrackerDAL.Models.Enums;

namespace TaskTrackerBLL.DTOs.Tasks
{
    public class ChangePriorityDto
    {
        [Required]
        public int TaskId { get; set; }

        [Required]
        public TaskPriority NewPriority { get; set; }
    }
}
