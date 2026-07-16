using TaskTrackerDAL.Models.Common;
using TaskTrackerDAL.Models.Enums;


namespace TaskTrackerDAL.Models
{
    public class ProjectTask : BaseModel
    {
        public int ProjectId { get; set; }

        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        public int? AssignedToUserId { get; set; }

        public ProjectTasksStatus Status { get; set; } = ProjectTasksStatus.NotStarted;

        public TaskPriority Priority { get; set; } = TaskPriority.Medium;

        public DateTime? DueDate { get; set; }

        public DateTime? CompletedAt { get; set; }

        public int CreatedByUserId { get; set; }

        // Navigation properties
        public Project Project { get; set; } = null!;

        public User? AssignedToUser { get; set; }

        public User CreatedByUser { get; set; } = null!;




    }
}
