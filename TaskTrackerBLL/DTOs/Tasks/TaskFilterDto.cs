using System;
using System.Collections.Generic;
using System.Text;
using TaskTrackerDAL.Models.Enums;

namespace TaskTrackerBLL.DTOs.Tasks
{
    public class TaskFilterDto
    {
        public int? AssignedToUserId {  get; set; }
        public TaskPriority? Priority { get; set; } 
        public ProjectTasksStatus? Status { get; set; }

        public int? ProjectId {  get; set; }
        public int? CompanyId { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
