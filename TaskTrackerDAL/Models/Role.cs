using TaskTrackerDAL.Models.Common;


namespace TaskTrackerDAL.Models
{
    public class Role:BaseModel
    {
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        // Navigation property
        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    }
}
