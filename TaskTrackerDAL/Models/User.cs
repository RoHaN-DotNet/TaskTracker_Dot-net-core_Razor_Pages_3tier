
using TaskTrackerDAL.Models.Common;


namespace TaskTrackerDAL.Models
{
    public class User:BaseModel
    {
        public int CompanyId { get; set; }

        public string FullName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string UserName { get; set; } = string.Empty;

        public string PasswordHash { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public DateTime? LastLoginAt { get; set; }

        // Navigation properties
        public Company Company { get; set; } = null!;

        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();

        public ICollection<ProjectMember> ProjectMemberships { get; set; } = new List<ProjectMember>();

        public ICollection<Project> CreatedProjects { get; set; } = new List<Project>();

        public ICollection<ProjectTask> AssignedTasks { get; set; } = new List<ProjectTask>();

        public ICollection<ProjectTask> CreatedTasks { get; set; } = new List<ProjectTask>();
    }
}
