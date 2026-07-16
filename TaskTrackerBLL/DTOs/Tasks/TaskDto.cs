using System;
using System.Collections.Generic;
using System.Text;
using TaskTrackerDAL.Models.Enums;

namespace TaskTrackerBLL.DTOs.Task
{
    public class TaskDto
    {
        public int Id { get; set; }

        public int ProjectId { get; set; }

        public string ProjectName { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        public int? AssignedToUserId { get; set; }

        public string? AssignedToUserName { get; set; }

        public ProjectTasksStatus Status { get; set; }

        public TaskPriority Priority { get; set; }

        public DateTime? DueDate { get; set; }

        public bool IsOverdue { get; set; }

        public DateTime? CompletedAt { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
