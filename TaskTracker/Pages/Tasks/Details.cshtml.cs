using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using TaskTrackerBLL.Authorization;

using TaskTrackerBLL.DTOs.Tasks;
using TaskTrackerBLL.Interfaces.Services;
using TaskTrackerBLL.Services;
using TaskTrackerDAL.Constants;
using TaskTrackerDAL.Models.Enums;

namespace TaskTracker.Pages.Tasks
{
    public class DetailsModel : PageModel
    {
        private readonly ITaskService _taskService;
        private readonly IProjectService _projectService;
        private readonly IAuthorizationService _authorizationService;
        public DetailsModel(ITaskService taskService, IProjectService projectService, IAuthorizationService authorizationService)
        {
            _taskService = taskService;
            _projectService = projectService;
            _authorizationService = authorizationService;
        }

        public TaskDto Task { get; set; } = default!;

        public IReadOnlyList<TaskProgressNoteDto> ProgressNotes { get; set; } = Array.Empty<TaskProgressNoteDto>();

        public bool CanManage { get; set; }

        public bool IsAssignedToMe { get; set; }

        [BindProperty]
        public AddProgressNoteDto NoteInput { get; set; } = new();

        [BindProperty]
        public AddCompletionCommentDto CompletionInput { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var taskResult = await _taskService.GetByIdAsync(id);

            if (!taskResult.Succeeded)
            {
                return NotFound();
            }

            var task = taskResult.Value!;
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            CanManage = User.IsInRole(AppRoles.Admin) || User.IsInRole(AppRoles.Manager);
            IsAssignedToMe = task.AssignedToUserIds.Contains(userId);

            if (CanManage)
            {
                var scopeCompanyId = User.IsInRole(AppRoles.Admin) ? (int?)null
                    : int.Parse(User.FindFirstValue("CompanyId")!);

                var projectResult = await _projectService.GetByIdAsync(task.ProjectId, scopeCompanyId);
                if (!projectResult.Succeeded)
                {
                    return Forbid();
                }
            }
            else if (!IsAssignedToMe)
            {
                return Forbid();
            }

            Task = task;

            var notesResult = await _taskService.GetProgressNotesAsync(id);
            ProgressNotes = notesResult.Succeeded ? notesResult.Value! : Array.Empty<TaskProgressNoteDto>();

            return Page();
        }

        public async Task<IActionResult> OnPostChangeStatusAsync(int id, ProjectTasksStatus newStatus)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            bool isManager = User.IsInRole(AppRoles.Manager);
            var dto = new MoveTaskStatusDto { TaskId = id, NewStatus = newStatus };
            var result = await _taskService.ChangeStatusAsync(dto, userId,isManager);

            if (!result.Succeeded)
            {
                TempData["TaskActionError"] = result.Error;
            }

            return RedirectToPage("/Tasks/Details", new { id });
        }

        public async Task<IActionResult> OnPostAddNoteAsync()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            if (!string.IsNullOrWhiteSpace(NoteInput.Note))
            {
                var result = await _taskService.AddProgressNoteAsync(NoteInput, userId);
                if (!result.Succeeded)
                {
                    TempData["TaskActionError"] = result.Error;
                }
            }

            return RedirectToPage("/Tasks/Details", new { id = NoteInput.TaskId });
        }

        public async Task<IActionResult> OnPostAddCompletionCommentAsync()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var result = await _taskService.AddCompletionCommentAsync(CompletionInput, userId);
            if (!result.Succeeded)
            {
                TempData["TaskActionError"] = result.Error;
            }

            return RedirectToPage("/Tasks/Details", new { id = CompletionInput.TaskId });
        }
        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var actingUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var scopeCompanyId = User.IsInRole(AppRoles.Admin)
                ? null
                : (int?)int.Parse(User.FindFirstValue("CompanyId")!);

            var result = await _taskService.DeleteAsync(id, actingUserId, scopeCompanyId);

            if (!result.Succeeded)
            {
                TempData["TaskActionError"] = result.Error;
            }

            return RedirectToPage("/Tasks/Details", new { id });
        }
        
    }
}
