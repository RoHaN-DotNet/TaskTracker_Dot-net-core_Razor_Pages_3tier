using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace TaskTrackerBLL.DTOs.Tasks
{
    public class AddProgressNoteDto
    {
        [Required]
        public int TaskId {  get; set; }
        [Required(ErrorMessage ="Please enter a note")]
        [StringLength(2000,MinimumLength =2,ErrorMessage ="Notes must be 2 and 2000 characters.")]
        public string Note { get; set; } = string.Empty;

    }
}
