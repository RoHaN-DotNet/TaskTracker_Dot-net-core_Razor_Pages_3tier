using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using TaskTrackerDAL.Models.Enums;

namespace TaskTrackerBLL.DTOs.Tasks
{
    public class CreateTaskDto
    {
            public int ProjectId { get; set; }

            public string Title { get; set; } = string.Empty;

            public string? Description { get; set; }

            public List<int> AssignedToUserIds { get; set; }= new();

            public ProjectTasksStatus Status { get; set; }

            public TaskPriority Priority { get; set; }

            public DateTime? DueDate { get; set; }
        
    }
}

