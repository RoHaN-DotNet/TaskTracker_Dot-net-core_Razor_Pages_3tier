using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

using TaskTrackerBLL.DTOs.TaskFile;
using TaskTrackerBLL.DTOs.Tasks;
using TaskTrackerBLL.Interfaces.Services;

using TaskTrackerDAL.Constants;

namespace TaskTracker.Pages.ProjectTasks
{
    public class DetailsModel : PageModel
    {
        private readonly ITaskService _taskService;
        private readonly IProjectService _projectService;
        private readonly ITaskFileService _taskFileService;

        public DetailsModel(
            ITaskService taskService,
            IProjectService projectService,
            ITaskFileService taskFileService)
        {
            _taskService = taskService;
            _projectService = projectService;
            _taskFileService = taskFileService;
        }
        // =========================================================
        // TASK
        // =========================================================
        [BindProperty]
        public TaskDto Task { get; set; } = new();
        // =========================================================
        // PROGRESS NOTES
        // =========================================================
        public IReadOnlyList<TaskProgressNoteDto> ProgressNotes { get; set; }
            = Array.Empty<TaskProgressNoteDto>();
        // =========================================================
        // PERMISSION
        // =========================================================
        public bool CanManage { get; set; }
        public bool IsAssignedToMe { get; set; }
        // =========================================================
        // NOTE
        // =========================================================
        [BindProperty]
        public AddProgressNoteDto NoteInput { get; set; } = new();
        // =========================================================
        // COMPLETION COMMENT
        // =========================================================
        [BindProperty]
        public AddCompletionCommentDto CompletionInput { get; set; } = new();
        // =========================================================
        // DEADLINE
        // =========================================================
        [BindProperty]
        public ChangeDeadlineDto Input { get; set; } = new();
        // =========================================================
        // FILES
        // =========================================================
        public IReadOnlyList<TaskFileDto> TaskFiles { get; set; }
            = new List<TaskFileDto>();
        [BindProperty]
        public IFormFile? UploadFile { get; set; }
        //Priority
        [BindProperty]
        public ChangePriorityDto Priority { get; set; } = new();
        // =========================================================
        // GET DETAILS PAGE
        // =========================================================
        public async Task<IActionResult> OnGetAsync(int id)
        {
            if (id <= 0)
            {
                return NotFound();
            }
            // -----------------------------------------------------
            // GET TASK
            // -----------------------------------------------------
            var taskResult =
                await _taskService.GetByIdAsync(id);
            if (!taskResult.Succeeded ||
                taskResult.Value == null)
            {
                return NotFound();
            }
            var task = taskResult.Value;
            // -----------------------------------------------------
            // CURRENT USER
            // -----------------------------------------------------
            var currentUserId =
                GetCurrentUserId();
            if (currentUserId == null)
            {
                return Challenge();
            }
            // -----------------------------------------------------
            // TASK FILES
            // -----------------------------------------------------
            var filesResult =
                await _taskFileService.GetByTaskIdAsync(id);
            if (filesResult.Succeeded &&
                filesResult.Value != null)
            {
                TaskFiles = filesResult.Value;
            }
            // -----------------------------------------------------
            // ROLE
            // -----------------------------------------------------
            CanManage =
                User.IsInRole(AppRoles.Admin) ||
                User.IsInRole(AppRoles.Manager);
            // -----------------------------------------------------
            // TASK MEMBER CHECK
            // -----------------------------------------------------
            IsAssignedToMe =
                task.AssignedToUserIds.Contains(
                    currentUserId.Value);
            // -----------------------------------------------------
            // AUTHORIZATION
            // -----------------------------------------------------

            int? companyId =
                User.IsInRole(AppRoles.Admin)
                    ? null
                    : GetCurrentCompanyId();
            if (CanManage)
            {
                var projectResult =
                    await _projectService.GetByIdAsync(
                        task.ProjectId,
                        companyId);
                if (!projectResult.Succeeded)
                {
                    return Forbid();
                }
            }
            else if (!IsAssignedToMe)
            {
                return Forbid();
            }
            // -----------------------------------------------------
            // SET TASK
            // -----------------------------------------------------
            Task = task;
            // -----------------------------------------------------
            // PROGRESS NOTES
            // -----------------------------------------------------
            var notesResult =
                await _taskService.GetProgressNotesAsync(id);
            ProgressNotes =
                notesResult.Succeeded &&
                notesResult.Value != null
                    ? notesResult.Value
                    : Array.Empty<TaskProgressNoteDto>();
            return Page();
        }
        // =========================================================
        // CHANGE DEADLINE
        // =========================================================
        public async Task<IActionResult> OnPostChangeDeadlineAsync()
        {
            if (Input.TaskId <= 0)
            {
                TempData["ErrorMessage"] =
                    "Task ID was missing from the form submission.";

                return RedirectToPage(
                    "/ProjectTasks/Details",
                    new { id = Input.TaskId });
            }
            if (Input.NewDueDate == default)
            {
                TempData["ErrorMessage"] =
                    "No deadline date was received from the form.";

                return RedirectToPage(
                    "/ProjectTasks/Details",
                    new { id = Input.TaskId });
            }
            // -----------------------------------------------------
            // GET TASK
            // -----------------------------------------------------
            var taskResult =
                await _taskService.GetByIdAsync(
                    Input.TaskId);
            if (!taskResult.Succeeded ||
                taskResult.Value == null)
            {
                return NotFound();
            }
            Task = taskResult.Value;
            // -----------------------------------------------------
            // NOTES
            // ----------------------------------------------------
            var notesResult =
                await _taskService.GetProgressNotesAsync(
                    Input.TaskId);
            ProgressNotes =
                notesResult.Succeeded &&
                notesResult.Value != null
                    ? notesResult.Value
                    : Array.Empty<TaskProgressNoteDto>();
            // -----------------------------------------------------
            // CURRENT USER
            // -----------------------------------------------------
            var currentUserId =
                GetCurrentUserId();
            if (currentUserId == null)
            {
                return Challenge();
            }
            // -----------------------------------------------------
            // ROLE
            // -----------------------------------------------------
            CanManage =
                User.IsInRole(AppRoles.Admin) ||
                User.IsInRole(AppRoles.Manager);
            IsAssignedToMe =
                Task.AssignedToUserIds.Contains(
                    currentUserId.Value);
            // -----------------------------------------------------
            // COMPANY
            // -----------------------------------------------------
            int? companyId =
                User.IsInRole(AppRoles.Admin)
                    ? null
                    : GetCurrentCompanyId();
            // -----------------------------------------------------
            // AUTHORIZATION
            // -----------------------------------------------------
            if (CanManage)
            {
                var projectResult =
                    await _projectService.GetByIdAsync(
                        Task.ProjectId,
                        companyId);
                if (!projectResult.Succeeded)
                {
                    TempData["ErrorMessage"] =
                        "You are not authorized to change this task's deadline.";

                    return RedirectToPage(
                        "/ProjectTasks/Details",
                        new { id = Input.TaskId });
                }
            }
            else if (!IsAssignedToMe)
            {
                TempData["ErrorMessage"] =
                    "You are not authorized to change this task's deadline.";
                return RedirectToPage(
                    "/ProjectTasks/Details",
                    new { id = Input.TaskId });
            }
            // -----------------------------------------------------
            // CHANGE DEADLINE
            // -----------------------------------------------------
            var result =
                await _taskService.ChangeDeadlineAsync(
                    Input,
                    companyId);
            if (!result.Succeeded)
            {
                TempData["ErrorMessage"] =
                    result.Error;

                return RedirectToPage(
                    "/ProjectTasks/Details",
                    new { id = Input.TaskId });
            }
            TempData["SuccessMessage"] =
                "Deadline updated successfully.";
            return RedirectToPage(
                "/ProjectTasks/Details",
                new { id = Input.TaskId });
        }
        // =========================================================
        // ADD FILE
        // =========================================================
        public async Task<IActionResult> OnPostAddFilesAsync(int id)
        {
            if (id <= 0)
            {
                TempData["ErrorMessage"] =
                    "Invalid task.";

                return RedirectToPage(
                    "/ProjectTasks/Details",
                    new { id });
            }
            if (UploadFile == null ||
                UploadFile.Length == 0)
            {
                TempData["ErrorMessage"] =
                    "Please select a file.";

                return RedirectToPage(
                    "/ProjectTasks/Details",
                    new { id });
            }
            var uploadFolder =
                Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot",
                    "uploads",
                    "tasks");
            var result =
                await _taskFileService.UploadAsync(
                    id,
                    UploadFile,
                    uploadFolder);
            if (!result.Succeeded)
            {
                TempData["ErrorMessage"] =
                    result.Error;

                return RedirectToPage(
                    "/ProjectTasks/Details",
                    new { id });
            }
            TempData["SuccessMessage"] =
                "File uploaded successfully.";
            return RedirectToPage(
                "/ProjectTasks/Details",
                new { id });
        }
        // =========================================================
        // ADD PROGRESS NOTE
        // =========================================================

        public async Task<IActionResult> OnPostAddNoteAsync()
        {
            var currentUserId =
                GetCurrentUserId();
            if (currentUserId == null)
            {
                TempData["TaskActionError"] =
                    "Unable to identify current user.";

                return RedirectToPage(
                    "/ProjectTasks/Details",
                    new { id = NoteInput.TaskId });
            }
            if (string.IsNullOrWhiteSpace(NoteInput.Note))
            {
                TempData["TaskActionError"] =
                    "Note cannot be empty.";

                return RedirectToPage(
                    "/ProjectTasks/Details",
                    new { id = NoteInput.TaskId });
            }
            var result =
                await _taskService.AddProgressNoteAsync(
                    NoteInput,
                    currentUserId.Value);
            if (!result.Succeeded)
            {
                TempData["TaskActionError"] =
                    result.Error;
            }
            else
            {
                TempData["SuccessMessage"] =
                    "Progress note added successfully.";
            }
            return RedirectToPage(
                "/ProjectTasks/Details",
                new { id = NoteInput.TaskId });
        }
        // =========================================================
        // GET CURRENT TASK MEMBERS
        // =========================================================
        //
        // Used by Assign/Reassign modal.
        //
        // Returns:
        //      projectId
        //      assignedUserIds
        //
        // =========================================================
        public async Task<IActionResult> OnGetTaskMembersAsync(
            int taskId)
        {
            if (taskId <= 0)
            {
                return new JsonResult(new
                {
                    success = false,
                    message = "Invalid task."
                });
            }
            // -----------------------------------------------------
            // GET TASK
            // -----------------------------------------------------
            var taskResult =
                await _taskService.GetByIdAsync(taskId);
            if (!taskResult.Succeeded ||
                taskResult.Value == null)
            {
                return new JsonResult(new
                {
                    success = false,
                    message = "Task not found."
                });
            }
            var task = taskResult.Value;
            // -----------------------------------------------------
            // ONLY ADMIN / MANAGER
            // -----------------------------------------------------
            var canManage =
                User.IsInRole(AppRoles.Admin) ||
                User.IsInRole(AppRoles.Manager);
            if (!canManage)
            {
                return new JsonResult(new
                {
                    success = false,
                    message = "You do not have permission to manage task members."
                });
            }
            // -----------------------------------------------------
            // RETURN CURRENT ASSIGNED MEMBERS
            // -----------------------------------------------------
            return new JsonResult(new
            {
                success = true,

                projectId = task.ProjectId,

                assignedUserIds =
                    task.AssignedToUserIds
                        .Distinct()
                        .ToList()
            });
        }
        // =========================================================
        // GET PROJECT MEMBERS
        // =========================================================
        public async Task<IActionResult> OnGetProjectMembersAsync(
            int projectId)
        {
            if (projectId <= 0)
            {
                return new JsonResult(
                    new List<object>());
            }
            // -----------------------------------------------------
            // GET PROJECT WITH MEMBERS
            // -----------------------------------------------------

            var project =
                await _projectService
                    .GetByIdWithMembersAsync(projectId);
            if (!project.Succeeded ||
                project.Value == null)
            {
                return new JsonResult(
                    new List<object>());
            }
            // -----------------------------------------------------
            // REMOVE ADMIN MEMBERS
            // -----------------------------------------------------

            var members =
                project.Value.Members
                    .Where(pm =>
                        !pm.Roles.Any(r =>
                            r.Equals(
                                AppRoles.Admin,
                                StringComparison.OrdinalIgnoreCase)))
                    .Select(pm => new
                    {
                        id = pm.UserId,

                        name = pm.FullName,

                        roles = pm.Roles
                    })
                    .ToList();
            return new JsonResult(members);
        }
        // =========================================================
        // ASSIGN / REASSIGN TASK MEMBERS
        // =========================================================
        public async Task<IActionResult> OnPostAssignMembersAsync(
            int taskId,
            List<int> assignedToUserIds)
        {
            // -----------------------------------------------------
            // TASK VALIDATION
            // -----------------------------------------------------
            if (taskId <= 0)
            {
                return new JsonResult(new
                {
                    success = false,
                    message = "Invalid task."
                });
            }
            // -----------------------------------------------------
            // MEMBERS
            // -----------------------------------------------------
            assignedToUserIds ??=
                new List<int>();
            assignedToUserIds =
                assignedToUserIds
                    .Distinct()
                    .ToList();
            // -----------------------------------------------------
            // CURRENT USER
            // -----------------------------------------------------
            var currentUserId =
                GetCurrentUserId();
            if (currentUserId == null)
            {
                return new JsonResult(new
                {
                    success = false,
                    message =
                        "Unable to identify current user. Please log in again."
                });
            }
            // -----------------------------------------------------
            // ONLY ADMIN / MANAGER
            // ----------------------------------------------------
            var canManage =
                User.IsInRole(AppRoles.Admin) ||
                User.IsInRole(AppRoles.Manager);
            if (!canManage)
            {
                return new JsonResult(new
                {
                    success = false,
                    message =
                        "You do not have permission to assign task members."
                });
            }
            // -----------------------------------------------------
            // GET TASK
            // -----------------------------------------------------
            var taskResult =
                await _taskService.GetByIdAsync(taskId);
            if (!taskResult.Succeeded ||
                taskResult.Value == null)
            {
                return new JsonResult(new
                {
                    success = false,
                    message = "Task not found."
                });
            }
            var task =
                taskResult.Value;
            // -----------------------------------------------------
            // COMPANY
            // -----------------------------------------------------
            int? companyId =
                User.IsInRole(AppRoles.Admin)
                    ? null
                    : GetCurrentCompanyId();
            // -----------------------------------------------------
            // CHECK PROJECT ACCESS
            // -----------------------------------------------------
            var projectResult =
                await _projectService.GetByIdAsync(
                    task.ProjectId,
                    companyId);
            if (!projectResult.Succeeded)
            {
                return new JsonResult(new
                {
                    success = false,
                    message =
                        "You are not authorized to manage this task."
                });
            }
            // -----------------------------------------------------
            // VALIDATE MEMBERS BELONG TO PROJECT
            // -----------------------------------------------------
            var projectWithMembers =
                await _projectService
                    .GetByIdWithMembersAsync(
                        task.ProjectId);
            if (!projectWithMembers.Succeeded ||
                projectWithMembers.Value == null)
            {
                return new JsonResult(new
                {
                    success = false,
                    message =
                        "Unable to load project members."
                });
            }
            var validProjectUserIds =
                projectWithMembers.Value.Members
                    .Where(pm =>
                        !pm.Roles.Any(r =>
                            r.Equals(
                                AppRoles.Admin,
                                StringComparison.OrdinalIgnoreCase)))
                    .Select(pm => pm.UserId)
                    .ToHashSet();
            // -----------------------------------------------------
            // INVALID USER IDS
            // -----------------------------------------------------
            var invalidUserIds =
                assignedToUserIds
                    .Where(id =>
                        !validProjectUserIds.Contains(id))
                    .ToList();
            if (invalidUserIds.Any())
            {
                return new JsonResult(new
                {
                    success = false,
                    message =
                        "One or more selected users are not members of this project."
                });
            }
            // -----------------------------------------------------
            // UPDATE TASK MEMBERS
            // -----------------------------------------------------
            var result =
                await _taskService.AssignMembersAsync(
                    taskId,
                    assignedToUserIds,
                    currentUserId.Value);
            if (!result.Succeeded)
            {
                return new JsonResult(new
                {
                    success = false,

                    message =
                        result.Error ??
                        "Unable to assign members."
                });
            }
            // -----------------------------------------------------
            // SUCCESS
            // -----------------------------------------------------

            return new JsonResult(new
            {
                success = true,

                message =
                    "Task members updated successfully."
            });
        }
        // =========================================================
        // HELPER
        // CURRENT USER ID
        // =========================================================
        private int? GetCurrentUserId()
        {
            var claim =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier);
            if (int.TryParse(
                claim,
                out int userId))
            {
                return userId;
            }
            return null;
        }
        // =========================================================
        // HELPER
        // CURRENT COMPANY ID
        // =========================================================
       private int GetCurrentCompanyId()
        {
            var claim =
                User.FindFirstValue(
                    "CompanyId");
            if (!int.TryParse(
                claim,
                out int companyId))
            {
                throw new InvalidOperationException(
                    "Current company ID could not be determined.");
            }
            return companyId;
        }
        // =========================================================
        // CHANGE PRIORITY - AJAX
        // =========================================================

        // =========================================================
        // CHANGE PRIORITY - AJAX
        // =========================================================

        public async Task<IActionResult> OnPostChangePriorityAsync(
            ChangePriorityDto dto)
        {
            // -----------------------------------------------------
            // VALIDATION
            // -----------------------------------------------------

            if (dto == null || dto.TaskId <= 0)
            {
                return new JsonResult(new
                {
                    success = false,
                    message = "Invalid task."
                })
                {
                    StatusCode = 400
                };
            }


            // -----------------------------------------------------
            // PERMISSION
            // -----------------------------------------------------

            var canManage =
                User.IsInRole(AppRoles.Admin) ||
                User.IsInRole(AppRoles.Manager);

            if (!canManage)
            {
                return new JsonResult(new
                {
                    success = false,
                    message =
                        "You do not have permission to change priority."
                })
                {
                    StatusCode = 403
                };
            }


            // -----------------------------------------------------
            // COMPANY
            // -----------------------------------------------------

            int? companyId =
                User.IsInRole(AppRoles.Admin)
                    ? null
                    : GetCurrentCompanyId();


            // -----------------------------------------------------
            // CHANGE PRIORITY
            // -----------------------------------------------------

            var result =
                await _taskService.ChangePriorityAsync(
                    dto,
                    companyId);


            // -----------------------------------------------------
            // FAILED
            // -----------------------------------------------------

            if (!result.Succeeded)
            {
                return new JsonResult(new
                {
                    success = false,
                    message =
                        result.Error ??
                        "Unable to update priority."
                })
                {
                    StatusCode = 400
                };
            }


            // -----------------------------------------------------
            // SUCCESS
            // -----------------------------------------------------

            return new JsonResult(new
            {
                success = true,

                priority = dto.NewPriority.ToString(),

                taskId = dto.TaskId
            });
        }

        
    }
}