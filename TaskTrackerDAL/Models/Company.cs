using TaskTrackerDAL.Models.Common;

namespace TaskTrackerDAL.Models
{
    public class Company:BaseModel
    {
        public string Name { get; set; } = string.Empty;

        public string? Email { get; set; }

        public string? Phone { get; set; }

        public string? Address { get; set; }

        public bool IsActive { get; set; } = true;

        // Navigation properties
        public ICollection<User> Users { get; set; } = new List<User>();

        public ICollection<Project> Projects { get; set; } = new List<Project>();
    }
}
