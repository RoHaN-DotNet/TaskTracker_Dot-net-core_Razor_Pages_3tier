using System;
using System.Collections.Generic;
using System.Text;
using TaskTrackerDAL.Models.Enums;

namespace TaskTrackerBLL.DTOs.Tasks
{
    public class MoveTaskStatusDto
    {
        public int TaskId { get; set; }

        public ProjectTasksStatus NewStatus { get; set; }
    }
}
