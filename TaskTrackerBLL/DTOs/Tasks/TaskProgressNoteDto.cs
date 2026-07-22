using System;
using System.Collections.Generic;
using System.Text;

namespace TaskTrackerBLL.DTOs.Tasks
{
    public class TaskProgressNoteDto
    {
        public int Id {  get; set; }
        public string AuthorName {  get; set; }=string.Empty;
        public string Note { get; set; } = string.Empty;
        public bool IsCompletionComment {  get; set; }
        public DateTime CreatedAt {  get; set; }
    }
}
