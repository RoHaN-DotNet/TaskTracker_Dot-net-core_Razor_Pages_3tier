using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace TaskTrackerBLL.DTOs.Tasks
{
    public class AssignTaskDto
    {
        [Required]
        public int TaskId {  get; set; }
        [Required(ErrorMessage = "Please select an employee.")]

        public int AssignedToUserId {  get; set; }
    }
}
