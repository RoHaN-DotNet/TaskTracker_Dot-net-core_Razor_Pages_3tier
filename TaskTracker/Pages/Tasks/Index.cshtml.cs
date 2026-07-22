using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using TaskTrackerBLL.DTOs.Task;
using TaskTrackerBLL.Interfaces.Services;

namespace TaskTracker.Pages.Tasks
{
    public class IndexModel : PageModel
    {
        private readonly ITaskService _taskService;

        public IndexModel(ITaskService taskService)
        {
            _taskService = taskService;
        }

        public IReadOnlyList<TaskDto> AssignedTasks { get; set; } = Array.Empty<TaskDto>();

        public async Task OnGetAsync()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _taskService.GetByAssignedUserIdAsync(userId);

            AssignedTasks = result.Succeeded ? result.Value! : Array.Empty<TaskDto>();
        }
    }
}
