using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using TaskTrackerBLL.DTOs.Tasks;
using TaskTrackerBLL.Interfaces.Services;
using TaskTrackerDAL.Constants;

namespace TaskTracker.Pages.Tasks
{
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Manager}")]
    public class ChangeDeadlineModel : PageModel
    {
        private readonly ITaskService _taskService;

        public ChangeDeadlineModel(ITaskService taskService)
        {
            _taskService = taskService;
        }

        [BindProperty]
        public ChangeDeadlineDto Input { get; set; } = new();

        public string TaskTitle { get; set; } = string.Empty;

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var taskResult = await _taskService.GetByIdAsync(id);
            if (!taskResult.Succeeded)
            {
                return NotFound();
            }

            Input.TaskId = id;
            Input.NewDueDate = taskResult.Value!.DueDate ?? DateTime.UtcNow.Date;
            TaskTitle = taskResult.Value!.Title;

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var scopeCompanyId = User.IsInRole(AppRoles.Admin)
                ? (int?)null
                : int.Parse(User.FindFirstValue("CompanyId")!);

            var result = await _taskService.ChangeDeadlineAsync(Input, scopeCompanyId);

            if (!result.Succeeded)
            {
                ModelState.AddModelError(string.Empty, result.Error!);
                return Page();
            }

            return RedirectToPage("/Tasks/Details", new { id = Input.TaskId });
        }
    }
}
