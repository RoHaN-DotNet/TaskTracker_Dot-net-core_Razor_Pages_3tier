using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace TaskTrackerBLL.DTOs.Tasks
{
    public class TransferTaskDto
    {
        
        
            [Required]
            public int TaskId { get; set; }

            [Required(ErrorMessage = "Please select who the task is being transferred from.")]
            public int FromUserId { get; set; }

            [Required(ErrorMessage = "Please select who the task is being transferred to.")]
            public int ToUserId { get; set; }

            [Required(ErrorMessage = "Please provide a reason for the transfer.")]
            [StringLength(1000, MinimumLength = 3)]
            public string Note { get; set; } = string.Empty;
    }
}


