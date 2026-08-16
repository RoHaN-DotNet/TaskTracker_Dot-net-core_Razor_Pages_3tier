using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using TaskTrackerBLL.Common;
using TaskTrackerBLL.DTOs.TaskFile;
using TaskTrackerBLL.DTOs.Tasks;
using TaskTrackerBLL.Interfaces.Services;
using TaskTrackerBLL.Services;
using TaskTrackerDAL.Constants;

namespace TaskTracker.Pages.ProjectTasks
{
    public class DetailsModel : PageModel
    {
        
        private readonly ITaskService _taskService;
        private readonly IProjectService _projectService;
        private readonly ITaskFileService _taskFileService;
        public DetailsModel(ITaskService taskService, IProjectService projectService, ITaskFileService taskFileService)
        {
            _taskService = taskService;
            _projectService = projectService;
            _taskFileService = taskFileService;
        }
        [BindProperty]
        public TaskDto Task { get; set; } = new();

        public IReadOnlyList<TaskProgressNoteDto> ProgressNotes { get; set; }
            = Array.Empty<TaskProgressNoteDto>();

        public bool CanManage { get; set; }

        public bool IsAssignedToMe { get; set; }

        [BindProperty]
        public AddProgressNoteDto NoteInput { get; set; } = new();

        [BindProperty]
        public AddCompletionCommentDto CompletionInput { get; set; } = new();

        [BindProperty]
        public ChangeDeadlineDto Input { get; set; } = new();
        public IReadOnlyList<TaskFileDto> TaskFiles { get; set; }= new List<TaskFileDto>();
        [BindProperty]
        public IFormFile? UploadFile { get; set; }
        public async Task<IActionResult> OnGetAsync(int id)
        {
            
            var taskResult = await _taskService.GetByIdAsync(id);

            if (!taskResult.Succeeded)
            {
                return NotFound();
            }
            var task = taskResult.Value!;
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            //Task Files
            var filesResult = await _taskFileService.GetByTaskIdAsync(id);

            if (filesResult.Succeeded)
            {
                TaskFiles = filesResult.Value!;
            }
            //end
            CanManage = User.IsInRole(AppRoles.Admin) || User.IsInRole(AppRoles.Manager);
            IsAssignedToMe = task.AssignedToUserId == userId;

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
        public async Task<IActionResult> OnPostChangeDeadlineAsync()
        {
            // Safety check: if TaskId or NewDueDate came in empty/zero,
            // the form field names don't match the DTO — fail loudly instead of silently.
            if (Input.TaskId <= 0)
            {
                TempData["ErrorMessage"] = "Task ID was missing from the form submission.";
                return RedirectToPage("/ProjectTasks/Details", new { id = Input.TaskId });
            }

            if (Input.NewDueDate == default)
            {
                TempData["ErrorMessage"] = "No deadline date was received from the form.";
                return RedirectToPage("/ProjectTasks/Details", new { id = Input.TaskId });
            }

            var taskResult = await _taskService.GetByIdAsync(Input.TaskId);

            if (!taskResult.Succeeded || taskResult.Value == null)
            {
                return NotFound();
            }

            Task = taskResult.Value;

            var notesResult = await _taskService.GetProgressNotesAsync(Input.TaskId);
            ProgressNotes = notesResult.Succeeded
                ? notesResult.Value!
                : Array.Empty<TaskProgressNoteDto>();

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            CanManage = User.IsInRole(AppRoles.Admin) || User.IsInRole(AppRoles.Manager);
            IsAssignedToMe = Task.AssignedToUserId == userId;

            int? companyId = User.IsInRole(AppRoles.Admin) ? null
                : int.Parse(User.FindFirstValue("CompanyId")!);

            if (CanManage)
            {
                var projectResult = await _projectService.GetByIdAsync(Task.ProjectId, companyId);
                if (!projectResult.Succeeded)
                {
                    TempData["ErrorMessage"] = "You are not authorized to change this task's deadline.";
                    return RedirectToPage("/ProjectTasks/Details", new { id = Input.TaskId });
                }
            }
            else if (!IsAssignedToMe)
            {
                TempData["ErrorMessage"] = "You are not authorized to change this task's deadline.";
                return RedirectToPage("/ProjectTasks/Details", new { id = Input.TaskId });
            }

            var result = await _taskService.ChangeDeadlineAsync(Input, companyId);

            if (!result.Succeeded)
            {
                TempData["ErrorMessage"] = result.Error;
                return RedirectToPage("/ProjectTasks/Details", new { id = Input.TaskId });
            }

            TempData["SuccessMessage"] = "Deadline updated successfully.";
            return RedirectToPage("/ProjectTasks/Details", new { id = Input.TaskId });
        }
        public async Task<IActionResult> OnPostAddFilesAsync(int id)
        {


            if (UploadFile != null && UploadFile.Length > 0)
            {
                var uploadFolder = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot",
                    "uploads",
                    "tasks"
                );

                var result = await _taskFileService.UploadAsync(
                    id,
                    UploadFile,
                    uploadFolder
                );

                if (!result.Succeeded)
                {
                    TempData["ErrorMessage"] = result.Error;

                    return RedirectToPage("/ProjectTasks/Details",new { id = id });
                }
            }

            TempData["SuccessMessage"] = "File uploaded successfully.";

            return RedirectToPage("/ProjectTasks/Details",new { id = id }
            );
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
            return RedirectToPage("/ProjectTasks/Details",new { id = NoteInput.TaskId });
        }
    }
}
