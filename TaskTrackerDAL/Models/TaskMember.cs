using System.ComponentModel.DataAnnotations.Schema;
using TaskTrackerDAL.Models.Common;

namespace TaskTrackerDAL.Models
{
    public class TaskMember
    {
        public int TaskId { get; set; }
        public int UserId {  get; set; }
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
        //Navigation property
        // Navigation properties
      
        public ProjectTask Task { get; set; } = null!;

        
        public User User { get; set; } = null!;


    }
}
