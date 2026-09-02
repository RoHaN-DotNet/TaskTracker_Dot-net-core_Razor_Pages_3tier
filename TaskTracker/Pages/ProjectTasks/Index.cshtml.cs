using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

using System.Security.Claims;

using TaskTrackerBLL.Common;
using TaskTrackerBLL.DTOs.Employee;
using TaskTrackerBLL.DTOs.Project;
using TaskTrackerBLL.DTOs.Tasks;
using TaskTrackerBLL.Interfaces.Services;

using TaskTrackerDAL.Constants;
using TaskTrackerDAL.Models.Enums;


namespace TaskTracker.Pages.ProjectTasks
{
    public class IndexModel : PageModel
    {
        private readonly ITaskService _taskService;
        private readonly IProjectService _projectService;
        private readonly IEmployeeService _employeeService;
        private readonly ITaskFileService _taskFileService;
        private readonly INotificationService _notificationService;


        public IndexModel(
            ITaskService taskService,
            IProjectService projectService,
            IEmployeeService employeeService,
            ITaskFileService taskFileService,
            INotificationService notificationService)
        {
            _taskService = taskService;
            _projectService = projectService;
            _employeeService = employeeService;
            _taskFileService = taskFileService;
            _notificationService = notificationService;
        }


        // =========================================================
        // TASK
        // =========================================================

        public PagedResult<TaskDto>? PagedTasks { get; set; }


        public IReadOnlyList<TaskDto> AssignedTasks { get; set; }
            = Array.Empty<TaskDto>();


        // =========================================================
        // CREATE TASK
        // =========================================================

        [BindProperty]
        public CreateTaskDto createTask { get; set; } = new();


        // =========================================================
        // MEMBER OPTIONS
        // =========================================================

        /*
         * Kept because your existing page may use this elsewhere.
         *
         * IMPORTANT:
         * Manage Members modal no longer uses this.
         * It loads members dynamically according to task's project.
         */

        public MultiSelectList MemberOption { get; set; }
            = new MultiSelectList(Array.Empty<object>());


        // =========================================================
        // PROJECT MEMBERS
        // =========================================================

        public IReadOnlyList<ProjectMemberDto> projectMembers { get; set; }
            = new List<ProjectMemberDto>();


        // =========================================================
        // FILE
        // =========================================================

        [BindProperty]
        public IFormFile? UploadFile { get; set; }


        // =========================================================
        // EMPLOYEES
        // =========================================================

        [BindProperty]
        public IReadOnlyList<EmployeeDto> AllEmployees { get; set; }
            = new List<EmployeeDto>();


        // =========================================================
        // STATUS OPTIONS
        // =========================================================

        public List<SelectListItem> StatusOptions { get; }
            = Enum.GetValues<ProjectTasksStatus>()
                .Select(status => new SelectListItem
                {
                    Value = ((int)status).ToString(),
                    Text = status.ToString()
                })
                .ToList();


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
        // EDIT / UPDATE TASK
        // =========================================================

        [BindProperty]
        public int EditTaskId { get; set; }


        [BindProperty]
        public ProjectTasksStatus EditStatus { get; set; }


        [BindProperty]
        public TaskPriority EditPriority { get; set; }


        [BindProperty]
        public DateTime? EditDueDate { get; set; }


        // =========================================================
        // MANAGE MEMBERS
        // =========================================================

        [BindProperty]
        public int TaskId { get; set; }


        [BindProperty]
        public List<int> AssignedToUserIds { get; set; }
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
            SetRoleFlags();

            ShowMyTasks = myTasks;


            int? companyId =
                IsAdmin
                    ? null
                    : GetCurrentCompanyId();


            // =====================================================
            // PROJECTS
            // =====================================================

            var projectResult =
                await _projectService.GetAllAsync(companyId);


            if (projectResult.Succeeded &&
                projectResult.Value != null)
            {
                Projects =
                    projectResult.Value.ToList();


                ProjectOptions =
                    Projects
                        .Select(p => new SelectListItem
                        {
                            Value = p.Id.ToString(),
                            Text = p.Name
                        })
                        .ToList();
            }


            // =====================================================
            // EMPLOYEES
            // =====================================================

            var employeeResult =
                await _employeeService.GetAllAsync(companyId);


            if (employeeResult.Succeeded &&
                employeeResult.Value != null)
            {
                AllEmployees =
                    employeeResult.Value.ToList();


                MemberOption =
                    new MultiSelectList(
                        AllEmployees,
                        "Id",
                        "FullName"
                    );
            }


            // =====================================================
            // EMPLOYEE TASKS
            // =====================================================

            if (IsEmployee)
            {
                var userId =
                    GetCurrentUserId();


                var result =
                    await _taskService
                        .GetByAssignedUserIdAsync(userId);


                if (result.Succeeded &&
                    result.Value != null)
                {
                    AssignedTasks =
                        result.Value.ToList();
                }


                return;
            }


            // =====================================================
            // MANAGER / ADMIN TASKS
            // =====================================================

            var filter =
                new TaskFilterDto
                {
                    CompanyId = companyId,
                    PageNumber = 1,
                    PageSize = 100
                };


            var taskResult =
                await _taskService
                    .FilterAsync(
                        filter,
                        companyId
                    );


            if (taskResult.Succeeded &&
                taskResult.Value != null)
            {
                PagedTasks =
                    taskResult.Value;
            }
        }


        // =========================================================
        // CREATE TASK
        // =========================================================

        public async Task<IActionResult> OnPostCreateTaskAsync()
        {
            var createdByUserId =
                GetCurrentUserId();


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
                    actingManagerCompanyId
                );


            // -----------------------------------------------------
            // CREATE FAILED
            // -----------------------------------------------------

            if (!result.Succeeded ||
                result.Value == null)
            {
                TempData["ErrorMessage"] =
                    result.Error ??
                    "Failed to create task.";


                return RedirectToPage();
            }


            var createdTask =
                result.Value;


            var taskId =
                createdTask.Id;


            // =====================================================
            // SEND NOTIFICATION TO ALL TASK MEMBERS
            // =====================================================

            foreach (
                var userId
                in createTask.AssignedToUserIds)
            {
                await _notificationService
                    .NotifyTaskAssignedAsync(
                        taskId,
                        userId,
                        createdTask.Title
                    );
            }


            // =====================================================
            // UPLOAD FILE
            // =====================================================

            if (UploadFile != null &&
                UploadFile.Length > 0)
            {
                var uploadFolder =
                    Path.Combine(
                        Directory.GetCurrentDirectory(),
                        "wwwroot",
                        "uploads",
                        "tasks"
                    );


                var uploadResult =
                    await _taskFileService
                        .UploadAsync(
                            taskId,
                            UploadFile,
                            uploadFolder
                        );


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
        // UPDATE TASK
        // =========================================================

        public async Task<IActionResult> OnPostUpdateAsync()
        {
            SetRoleFlags();


            // Only Manager/Admin can update task
            if (!IsManager && !IsAdmin)
            {
                return Forbid();
            }


            int? companyId =
                IsAdmin
                    ? null
                    : GetCurrentCompanyId();


            // =====================================================
            // CHANGE STATUS
            // =====================================================

            var statusDto =
                new MoveTaskStatusDto
                {
                    TaskId = EditTaskId,
                    NewStatus = EditStatus
                };


            var statusResult =
                await _taskService
                    .ChangeStatusAsync(
                        statusDto,
                        GetCurrentUserId(),
                        IsManager
                    );


            if (!statusResult.Succeeded)
            {
                ModelState.AddModelError(
                    string.Empty,
                    statusResult.Error ??
                    "Unable to update task status."
                );


                await LoadPageDataAsync(companyId);


                return Page();
            }


            // =====================================================
            // CHANGE PRIORITY
            // =====================================================

            var priorityDto =
                new ChangePriorityDto
                {
                    TaskId = EditTaskId,
                    NewPriority = EditPriority
                };


            var priorityResult =
                await _taskService
                    .ChangePriorityAsync(
                        priorityDto,
                        companyId
                    );


            if (!priorityResult.Succeeded)
            {
                ModelState.AddModelError(
                    string.Empty,
                    priorityResult.Error ??
                    "Unable to update task priority."
                );


                await LoadPageDataAsync(companyId);


                return Page();
            }


            // =====================================================
            // CHANGE DEADLINE
            // =====================================================

            if (!EditDueDate.HasValue)
            {
                ModelState.AddModelError(
                    nameof(EditDueDate),
                    "Due date is required."
                );


                await LoadPageDataAsync(companyId);


                return Page();
            }


            var deadlineDto =
                new ChangeDeadlineDto
                {
                    TaskId = EditTaskId,
                    NewDueDate = EditDueDate.Value
                };


            var deadlineResult =
                await _taskService
                    .ChangeDeadlineAsync(
                        deadlineDto,
                        companyId
                    );


            if (!deadlineResult.Succeeded)
            {
                ModelState.AddModelError(
                    string.Empty,
                    deadlineResult.Error ??
                    "Unable to update task deadline."
                );


                await LoadPageDataAsync(companyId);


                return Page();
            }


            // =====================================================
            // SUCCESS
            // =====================================================

            return RedirectToPage();
        }


        // =========================================================
        // MANAGE TASK MEMBERS
        // =========================================================

        public async Task<IActionResult> OnPostAssignMembersAsync()
        {
            SetRoleFlags();


            // Only Manager can manage members
            if (!IsManager)
            {
                return Forbid();
            }


            // -----------------------------------------------------
            // BASIC VALIDATION
            // -----------------------------------------------------

            if (TaskId <= 0)
            {
                TempData["ErrorMessage"] =
                    "Invalid task.";


                return RedirectToPage();
            }


            // -----------------------------------------------------
            // REMOVE DUPLICATES
            // -----------------------------------------------------

            AssignedToUserIds =
                AssignedToUserIds?
                    .Distinct()
                    .ToList()
                ?? new List<int>();


            // -----------------------------------------------------
            // ASSIGN / REASSIGN MEMBERS
            // -----------------------------------------------------

            var result =
                await _taskService
                    .AssignMembersAsync(
                        TaskId,
                        AssignedToUserIds,
                        GetCurrentUserId()
                    );


            // -----------------------------------------------------
            // FAILED
            // -----------------------------------------------------

            if (!result.Succeeded)
            {
                TempData["ErrorMessage"] =
                    result.Error ??
                    "Failed to update task members.";


                return RedirectToPage();
            }


            // -----------------------------------------------------
            // SUCCESS
            // -----------------------------------------------------

            TempData["SuccessMessage"] =
                "Task members updated successfully.";


            return RedirectToPage();
        }


        // =========================================================
        // GET CURRENT TASK MEMBERS
        // =========================================================

        public async Task<IActionResult> OnGetTaskMembersAsync(
            int taskId)
        {
            if (taskId <= 0)
            {
                return new JsonResult(
                    new List<int>()
                );
            }


            var result =
                await _taskService
                    .GetByIdAsync(taskId);


            if (!result.Succeeded ||
                result.Value == null)
            {
                return new JsonResult(
                    new List<int>()
                );
            }


            var assignedUserIds =
                result.Value.AssignedToUserIds?
                    .Distinct()
                    .ToList()
                ?? new List<int>();


            return new JsonResult(
                assignedUserIds
            );
        }


        // =========================================================
        // LOAD PAGE DATA
        // =========================================================

        private async Task LoadPageDataAsync(
            int? companyId)
        {
            // -----------------------------------------------------
            // Projects
            // -----------------------------------------------------

            var projectResult =
                await _projectService
                    .GetAllAsync(companyId);


            if (projectResult.Succeeded &&
                projectResult.Value != null)
            {
                Projects =
                    projectResult.Value.ToList();


                ProjectOptions =
                    Projects
                        .Select(p => new SelectListItem
                        {
                            Value = p.Id.ToString(),
                            Text = p.Name
                        })
                        .ToList();
            }


            // -----------------------------------------------------
            // Employees
            // -----------------------------------------------------

            var employeeResult =
                await _employeeService
                    .GetAllAsync(companyId);


            if (employeeResult.Succeeded &&
                employeeResult.Value != null)
            {
                AllEmployees =
                    employeeResult.Value.ToList();


                MemberOption =
                    new MultiSelectList(
                        AllEmployees,
                        "Id",
                        "FullName"
                    );
            }


            // -----------------------------------------------------
            // Employee
            // -----------------------------------------------------

            if (IsEmployee)
            {
                var userId =
                    GetCurrentUserId();


                var result =
                    await _taskService
                        .GetByAssignedUserIdAsync(
                            userId
                        );


                if (result.Succeeded &&
                    result.Value != null)
                {
                    AssignedTasks =
                        result.Value.ToList();
                }


                return;
            }


            // -----------------------------------------------------
            // Manager / Admin
            // -----------------------------------------------------

            var filter =
                new TaskFilterDto
                {
                    CompanyId = companyId,
                    PageNumber = 1,
                    PageSize = 100
                };


            var taskResult =
                await _taskService
                    .FilterAsync(
                        filter,
                        companyId
                    );


            if (taskResult.Succeeded &&
                taskResult.Value != null)
            {
                PagedTasks =
                    taskResult.Value;
            }
        }


        // =========================================================
        // ROLE FLAGS
        // =========================================================

        private void SetRoleFlags()
        {
            IsAdmin =
                User.IsInRole(
                    AppRoles.Admin
                );


            IsManager =
                User.IsInRole(
                    AppRoles.Manager
                );


            IsEmployee =
                !IsAdmin &&
                !IsManager;
        }


        // =========================================================
        // CURRENT USER ID
        // =========================================================

        private int GetCurrentUserId()
        {
            var userId =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier
                );


            if (!int.TryParse(
                    userId,
                    out var id))
            {
                throw new InvalidOperationException(
                    "Current user ID could not be determined."
                );
            }


            return id;
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
                    new List<object>()
                );
            }


            var project =
                await _projectService
                    .GetByIdWithMembersAsync(
                        projectId
                    );


            if (!project.Succeeded ||
                project.Value == null)
            {
                return new JsonResult(
                    new List<object>()
                );
            }


            var members =
                project.Value.Members
                    .Where(pm =>
                        !(pm.Roles?.Any(r =>
                            r.Equals(
                                AppRoles.Admin,
                                StringComparison.OrdinalIgnoreCase
                            )
                        ) ?? false)
                    )
                    .Select(pm => new
                    {
                        id = pm.UserId,
                        name = pm.FullName,
                        roles = pm.Roles
                    })
                    .ToList();


            return new JsonResult(
                members
            );
        }


        // =========================================================
        // CURRENT COMPANY ID
        // =========================================================

        private int GetCurrentCompanyId()
        {
            var companyId =
                User.FindFirstValue(
                    "CompanyId"
                );


            if (!int.TryParse(
                    companyId,
                    out var id))
            {
                throw new InvalidOperationException(
                    "Current company ID could not be determined."
                );
            }


            return id;
        }
    }
}