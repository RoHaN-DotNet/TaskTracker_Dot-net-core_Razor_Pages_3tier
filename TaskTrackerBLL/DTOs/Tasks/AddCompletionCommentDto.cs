using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace TaskTrackerBLL.DTOs.Tasks
{
    public class AddCompletionCommentDto
    {
        [Required]
        public int TaskId {  get; set; }
        [Required(ErrorMessage = "Please describe what was completed.")]
        [StringLength(2000, MinimumLength = 5, ErrorMessage = "Completion comments must be between 5 and 2000 characters.")]
        public string Comment { get; set; } = string.Empty;
    }
}
