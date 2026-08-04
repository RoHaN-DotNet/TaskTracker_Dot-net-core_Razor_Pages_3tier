using System;
using System.Collections.Generic;
using System.Text;

namespace TaskTrackerBLL.DTOs.Tasks
{
    public class TaskDetailsDto
    {
        public TaskDto Task { get; set; } = new();

        public IReadOnlyList<TaskProgressNoteDto> Notes { get; set; }
            = Array.Empty<TaskProgressNoteDto>();
    }
}
