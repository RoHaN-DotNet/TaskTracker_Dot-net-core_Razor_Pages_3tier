using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace TaskTrackerBLL.DTOs.Tasks
{
    public class ChangeDeadlineDto
    {
        [Required]
        public int TaskId { get; set; }

        [Required(ErrorMessage = "Please select a due date.")]
        [DataType(DataType.Date)]
        public DateTime NewDueDate { get; set; }

    }
}
