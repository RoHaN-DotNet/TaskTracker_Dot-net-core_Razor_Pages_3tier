using System.ComponentModel.DataAnnotations;

namespace TaskTrackerBLL.DTOs.Project
{
    public class AssignProjectMemberDto
    {
        [Required]
        public int ProjectId { get; set; }

        [Required(ErrorMessage = "Please select an employee.")]
        public int UserId { get; set; }
    }
}
