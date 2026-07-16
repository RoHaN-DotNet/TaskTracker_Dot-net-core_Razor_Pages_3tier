using TaskTrackerDAL.Models.Common;
using TaskTrackerDAL.Models.Enums;

namespace TaskTrackerDAL.Models
{
    public class Project:BaseModel
    {
        public int CompanyId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public ProjectStatus Status { get; set; } = ProjectStatus.NotStarted;

        public int CreatedByUserId { get; set; }

        // Navigation properties
        public Company Company { get; set; } = null!;

        public User CreatedByUser { get; set; } = null!;

        public ICollection<ProjectMember> ProjectMembers { get; set; } = new List<ProjectMember>();

        public ICollection<ProjectTask> Tasks { get; set; } = new List<ProjectTask>();
    }
}
