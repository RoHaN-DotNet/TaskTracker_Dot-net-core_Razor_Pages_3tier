using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Security.Claims;

using TaskTrackerBLL.DTOs.Employee;
using TaskTrackerBLL.DTOs.Project;
using TaskTrackerBLL.DTOs.Tasks;
using TaskTrackerBLL.Interfaces.Services;

using TaskTrackerDAL.Constants;
using TaskTrackerDAL.Models.Enums;

namespace TaskTracker.Pages.ProjectTasks
{
    public class TasksModel : PageModel
    {
        private readonly ITaskService _taskService;
        private readonly IEmployeeService _employeeService;
        private readonly IProjectService _projectService;
        private readonly ITaskFileService _taskFileService;
        private readonly INotificationService _notificationService;

        public TasksModel(
            ITaskService taskService,
            IEmployeeService employeeService,
            IProjectService projectService,
            ITaskFileService taskFileService,
            INotificationService notificationService)
        {
            _taskService = taskService;
            _employeeService = employeeService;
            _projectService = projectService;
            _taskFileService = taskFileService;
            _notificationService = notificationService;
        }

        // =========================================================
        // TASK COLUMNS
        // =========================================================

        public List<TaskDto> NotStartedTasks { get; set; } = new();

        public List<TaskDto> InProgressTasks { get; set; } = new();

        public List<TaskDto> OnHoldTasks { get; set; } = new();

        public List<TaskDto> CompletedTasks { get; set; } = new();

        public List<TaskDto> CancelledTasks { get; set; } = new();

        public List<TaskDto> ArchivedTasks { get; set; } = new();


        // =========================================================
        // CREATE TASK
        // =========================================================

        [BindProperty]
        public CreateTaskDto createTask { get; set; } = new();


        // =========================================================
        // MEMBERS
        // =========================================================

        public MultiSelectList MemberOption { get; set; }
            = new MultiSelectList(Array.Empty<object>());
        // =========================================================
        // MEMBERS
        // =========================================================
        public IReadOnlyList<ProjectMemberDto> ProjectMembers { get; set; } = new List<ProjectMemberDto>();

        // =========================================================
        // FILE
        // =========================================================

        [BindProperty]
        public IFormFile? UploadFile { get; set; }


        // =========================================================
        // EMPLOYEES
        // =========================================================

        [BindProperty]
        public IReadOnlyList<EmployeeDto> AllEmployees { get; set; } = new List<EmployeeDto>();
        // =========================================================
        // TASK DETAILS ACCESS
        // =========================================================
        public HashSet<int> ViewableTaskIds { get; private set; } = new();

        // =========================================================
        // STATUS
        // =========================================================

        [BindProperty]
        public MoveTaskStatusDto MoveTaskStatus { get; set; }
            = new();


        // =========================================================
        // PROJECTS
        // =========================================================

        [BindProperty]
        public List<ProjectDto> Projects { get; set; }
            = new();

        public List<SelectListItem> ProjectOptions { get; set; }
            = new();


        // =========================================================
        // PROGRESS NOTE
        // =========================================================

        [BindProperty]
        public AddProgressNoteDto NoteInput { get; set; }
            = new();


        // =========================================================
        // DEADLINE
        // =========================================================

        [BindProperty]
        public ChangeDeadlineDto DeadlineInput { get; set; }
            = new();


        // =========================================================
        // USER ROLE FLAGS
        // =========================================================

        public bool IsEmployee { get; private set; }

        public bool IsManager { get; private set; }

        public bool IsAdmin { get; private set; }

        public bool ShowMyTasks { get; private set; }


        // =========================================================
        // GET
        // =========================================================

        public async Task OnGetAsync(bool myTasks = false)
        {
            // -----------------------------------------------------
            // ROLE
            // -----------------------------------------------------

            IsAdmin = User.IsInRole(AppRoles.Admin);

            IsManager = User.IsInRole(AppRoles.Manager);

            IsEmployee = !IsAdmin && !IsManager;


            // -----------------------------------------------------
            // TOGGLE
            // false = ALL TASKS
            // true  = MY TASKS
            // -----------------------------------------------------

            ShowMyTasks = myTasks;


            // -----------------------------------------------------
            // COMPANY
            // -----------------------------------------------------

            int? companyId = User.IsInRole(AppRoles.Admin)
                ? null
                : GetCurrentCompanyId();


            // =====================================================
            // PROJECTS
            // =====================================================

            var projectResult =
                await _projectService.GetAllAsync(companyId);

            if (projectResult.Succeeded && projectResult.Value != null)
            {
                Projects = projectResult.Value.ToList();
            }


            // =====================================================
            // EMPLOYEES
            // =====================================================

            var employeeResult =
                await _employeeService.GetAllAsync(companyId);

            if (employeeResult.Succeeded && employeeResult.Value != null)
            {
                AllEmployees = employeeResult.Value.ToList();
            }


            // =====================================================
            // EMPLOYEE
            // =====================================================

            if (IsEmployee)
            {
                var userId = GetCurrentUserId();

                // -------------------------------------------------
                // GET TASKS ASSIGNED TO THIS EMPLOYEE
                // -------------------------------------------------

                var assignedResult =
                    await _taskService.GetByAssignedUserIdAsync(userId);

                var assignedTasks =
                    assignedResult.Succeeded &&
                    assignedResult.Value != null
                        ? assignedResult.Value.ToList()
                        : new List<TaskDto>();


                // These are the ONLY task details the employee can open
                ViewableTaskIds = assignedTasks
                    .Select(t => t.Id)
                    .ToHashSet();


                List<TaskDto> tasks;


                // -------------------------------------------------
                // MY TASKS
                // -------------------------------------------------

                if (ShowMyTasks)
                {
                    tasks = assignedTasks;
                }

                // -------------------------------------------------
                // ALL COMPANY TASKS
                // -------------------------------------------------

                else
                {
                    var filter = new TaskFilterDto
                    {
                        CompanyId = companyId,
                        PageNumber = 1,
                        PageSize = 1000
                    };

                    var result =
                        await _taskService.FilterAsync(
                            filter,
                            companyId);

                    if (!result.Succeeded || result.Value == null)
                    {
                        return;
                    }

                    tasks = result.Value.Items.ToList();
                }


                // -------------------------------------------------
                // PUT TASKS INTO COLUMNS
                // -------------------------------------------------

                SetTaskColumns(tasks);
            }


            // =====================================================
            // MANAGER
            // =====================================================

            // =====================================================
            // MANAGER
            // =====================================================

            else if (IsManager)
            {
                var filter = new TaskFilterDto
                {
                    CompanyId = companyId,
                    PageNumber = 1,
                    PageSize = 1000
                };

                var result =
                    await _taskService.FilterAsync(
                        filter,
                        companyId);

                if (!result.Succeeded || result.Value == null)
                {
                    return;
                }

                var tasks = result.Value.Items.ToList();

                // Manager can view every task in their company
                ViewableTaskIds = tasks
                    .Select(t => t.Id)
                    .ToHashSet();

                SetTaskColumns(tasks);
            }


            // =====================================================
            // ADMIN
            // =====================================================

            else if (IsAdmin)
            {
                var filter = new TaskFilterDto
                {
                    CompanyId = companyId,
                    PageNumber = 1,
                    PageSize = 1000
                };

                var result =
                    await _taskService.FilterAsync(
                        filter,
                        companyId);

                if (!result.Succeeded || result.Value == null)
                {
                    return;
                }

                var tasks = result.Value.Items.ToList();

                SetTaskColumns(tasks);
            }
        }


        // =========================================================
        // CREATE TASK
        // =========================================================

        public async Task<IActionResult> OnPostCreateTaskAsync()
        {
            var createdByUserId = GetCurrentUserId();

            int? actingManagerCompanyId =
                User.IsInRole(AppRoles.Admin)
                    ? null
                    : GetCurrentCompanyId();


            // -----------------------------------------------------
            // BASIC VALIDATION
            // -----------------------------------------------------

            if (createTask.ProjectId <= 0)
            {
                TempData["ErrorMessage"] =
                    "Please select a project.";

                return RedirectToPage();
            }

            if (string.IsNullOrWhiteSpace(createTask.Title))
            {
                TempData["ErrorMessage"] =
                    "Task title is required.";

                return RedirectToPage();
            }


            // -----------------------------------------------------
            // REMOVE DUPLICATE MEMBERS
            // -----------------------------------------------------

            createTask.AssignedToUserIds =
                createTask.AssignedToUserIds?
                    .Distinct()
                    .ToList()
                ?? new List<int>();


            // =====================================================
            // CREATE TASK
            // =====================================================

            var result =
                await _taskService.CreateAsync(
                    createTask,
                    createdByUserId,
                    actingManagerCompanyId);


            // -----------------------------------------------------
            // CREATE FAILED
            // -----------------------------------------------------

            if (!result.Succeeded || result.Value == null)
            {
                TempData["ErrorMessage"] =
                    result.Error ?? "Failed to create task.";

                return RedirectToPage();
            }


            var createdTask = result.Value;

            var taskId = createdTask.Id;


            // =====================================================
            // SEND NOTIFICATION TO ALL TASK MEMBERS
            // =====================================================

            foreach (var userId in createTask.AssignedToUserIds)
            {
                await _notificationService.NotifyTaskAssignedAsync(
                    taskId,
                    userId,
                    createdTask.Title);
            }


            // =====================================================
            // UPLOAD FILE
            // =====================================================

            if (UploadFile != null && UploadFile.Length > 0)
            {
                var uploadFolder = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot",
                    "uploads",
                    "tasks"
                );

                var uploadResult =
                    await _taskFileService.UploadAsync(
                        taskId,
                        UploadFile,
                        uploadFolder);


                if (!uploadResult.Succeeded)
                {
                    TempData["ErrorMessage"] =
                        uploadResult.Error ??
                        "Task created but file upload failed.";

                    return RedirectToPage();
                }
            }


            // =====================================================
            // SUCCESS
            // =====================================================

            TempData["SuccessMessage"] =
                "Task created successfully.";

            return RedirectToPage();
        }


        // =========================================================
        // ADD PROGRESS NOTE
        // =========================================================

        public async Task<IActionResult> OnPostAddNoteAsync()
        {
            var userId = GetCurrentUserId();


            if (string.IsNullOrWhiteSpace(NoteInput.Note))
            {
                TempData["TaskActionError"] =
                    "Note cannot be empty.";

                return RedirectToPage();
            }


            var result =
                await _taskService.AddProgressNoteAsync(
                    NoteInput,
                    userId);


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


            return RedirectToPage();
        }


        // =========================================================
        // UPDATE TASK STATUS
        // =========================================================

        [HttpPost("Update-TaskStatus")]
        public async Task<IActionResult> OnPostUpdateTaskStatusAsync(
            [FromBody] MoveTaskStatusDto moveTask)
        {
            var actingUserId =
                GetCurrentUserId();


            var isManager =
                User.IsInRole(AppRoles.Manager) ||
                User.IsInRole(AppRoles.Admin);


            var result =
                await _taskService.ChangeStatusAsync(
                    moveTask,
                    actingUserId,
                    isManager);


            if (!result.Succeeded)
            {
                return new JsonResult(new
                {
                    success = false,
                    message = result.Error
                });
            }


            return new JsonResult(new
            {
                success = true
            });
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

                return RedirectToPage();
            }


            if (UploadFile == null || UploadFile.Length == 0)
            {
                TempData["ErrorMessage"] =
                    "Please select a file.";

                return RedirectToPage();
            }


            var uploadFolder = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot",
                "uploads",
                "tasks"
            );


            var result =
                await _taskFileService.UploadAsync(
                    id,
                    UploadFile,
                    uploadFolder);


            if (!result.Succeeded)
            {
                TempData["ErrorMessage"] =
                    result.Error;

                return RedirectToPage();
            }


            TempData["SuccessMessage"] =
                "File uploaded successfully.";


            return RedirectToPage();
        }


        // =========================================================
        // CHANGE DEADLINE
        // =========================================================

        public async Task<IActionResult>
            OnPostChangeDeadlineFromBoardAsync()
        {
            if (DeadlineInput.TaskId <= 0)
            {
                TempData["ErrorMessage"] =
                    "Invalid task.";

                return RedirectToPage();
            }


            if (DeadlineInput.NewDueDate == default)
            {
                TempData["ErrorMessage"] =
                    "Please select a deadline.";

                return RedirectToPage();
            }


            // -----------------------------------------------------
            // GET TASK
            // -----------------------------------------------------

            var taskResult =
                await _taskService.GetByIdAsync(
                    DeadlineInput.TaskId);


            if (!taskResult.Succeeded ||
                taskResult.Value == null)
            {
                return NotFound();
            }


            // -----------------------------------------------------
            // ROLE
            // -----------------------------------------------------

            bool canManage =
                User.IsInRole(AppRoles.Admin) ||
                User.IsInRole(AppRoles.Manager);


            /*
             * IMPORTANT:
             *
             * আগের code এখানে:
             *
             * task.AssignedToUserId == userId
             *
             * check করছিল।
             *
             * কিন্তু এখন একাধিক TaskMember আছে।
             *
             * তাই পুরোনো single AssignedToUserId
             * logic এখানে রাখা যাবে না।
             *
             * আপাতত deadline change শুধু
             * Admin / Manager করতে পারবে।
             *
             * Employee-এর জন্য TaskMember based permission
             * আমরা TaskService-এর পরের step-এ properly add করব।
             */

            if (!canManage)
            {
                return Forbid();
            }


            // -----------------------------------------------------
            // COMPANY
            // -----------------------------------------------------

            int? companyId =
                User.IsInRole(AppRoles.Admin)
                    ? null
                    : GetCurrentCompanyId();


            // -----------------------------------------------------
            // CHANGE DEADLINE
            // -----------------------------------------------------

            var result =
                await _taskService.ChangeDeadlineAsync(
                    DeadlineInput,
                    companyId);


            if (!result.Succeeded)
            {
                TempData["ErrorMessage"] =
                    result.Error;

                return RedirectToPage();
            }


            TempData["SuccessMessage"] =
                "Deadline updated successfully.";


            return RedirectToPage();
        }


        // =========================================================
        // HELPER: SET TASK COLUMNS
        // =========================================================

        private void SetTaskColumns(
            IEnumerable<TaskDto> tasks)
        {
            var taskList = tasks.ToList();


            NotStartedTasks = taskList
                .Where(t =>
                    t.Status ==
                    ProjectTasksStatus.NotStarted)
                .ToList();


            InProgressTasks = taskList
                .Where(t =>
                    t.Status ==
                    ProjectTasksStatus.InProgress)
                .ToList();


            OnHoldTasks = taskList
                .Where(t =>
                    t.Status ==
                    ProjectTasksStatus.OnHold)
                .ToList();


            CompletedTasks = taskList
                .Where(t =>
                    t.Status ==
                    ProjectTasksStatus.Completed)
                .ToList();


            CancelledTasks = taskList
                .Where(t =>
                    t.Status ==
                    ProjectTasksStatus.Cancelled)
                .ToList();


            ArchivedTasks = taskList
                .Where(t =>
                    t.Status ==
                    ProjectTasksStatus.Archived)
                .ToList();
        }


        // =========================================================
        // HELPER: CURRENT USER ID
        // =========================================================

        private int GetCurrentUserId()
        {
            var userId =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

            if (!int.TryParse(userId, out var id))
            {
                throw new InvalidOperationException(
                    "Current user ID could not be determined.");
            }

            return id;
        }


        // =========================================================
        // HELPER: CURRENT COMPANY ID
        // =========================================================

        private int GetCurrentCompanyId()
        {
            var companyId =
                User.FindFirstValue("CompanyId");

            if (!int.TryParse(companyId, out var id))
            {
                throw new InvalidOperationException(
                    "Current company ID could not be determined.");
            }

            return id;
        }
        // =========================================================
        // GETTING ALL THE PROJECT MEMBERS
        // =========================================================
        public async Task<IActionResult> OnGetProjectMembersAsync(int projectId)
        {
            if (projectId <= 0)
            {
                return new JsonResult(new List<object>());
            }

            var project =
                await _projectService.GetByIdWithMembersAsync(projectId);

            if (!project.Succeeded || project.Value == null)
            {
                return new JsonResult(new List<object>());
            }

            var members = project.Value.Members
                .Where(pm => !pm.Roles.Any(r =>
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
    }
}