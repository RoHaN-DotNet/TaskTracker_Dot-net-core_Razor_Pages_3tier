using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Identity.Client;
using System.Security.Claims;
using TaskTrackerBLL.DTOs.Employee;
using TaskTrackerBLL.DTOs.Project;
using TaskTrackerBLL.DTOs.Tasks;
using TaskTrackerBLL.Interfaces.Services;
using TaskTrackerBLL.Services;
using TaskTrackerDAL.Constants;
using TaskTrackerDAL.Models.Enums;

namespace TaskTracker.Pages.ProjectTasks
{
    public class TasksModel : PageModel
    {
        private readonly ITaskService _taskService;
        private readonly IEmployeeService _employeeService;
        private readonly IProjectService _projectService;
        public TasksModel(ITaskService taskService, IEmployeeService employeeService, IProjectService projectService)
        {
            _taskService = taskService;
            _employeeService = employeeService;
            _projectService = projectService;
        }

        public List<TaskDto> NotStartedTasks { get; set; } = new();

        public List<TaskDto> InProgressTasks { get; set; } = new();

        public List<TaskDto> OnHoldTasks { get; set; } = new();

        public List<TaskDto> CompletedTasks { get; set; } = new();
        public List<TaskDto> CancelledTasks { get; set; } = new();

        public List<TaskDto> ArchivedTasks { get; set; } = new();
        [BindProperty]
        public CreateTaskDto createTask {  get; set; }= new();
        public MultiSelectList MemberOption { get; set; } = new(Array.Empty<object>());
        [BindProperty]
        public IReadOnlyList<EmployeeDto> AllEmployees { get; set; } = new List<EmployeeDto>();
        [BindProperty]
        public MoveTaskStatusDto MoveTaskStatus { get; set; } = new();
        [BindProperty]
        public List<ProjectDto> Projects { get; set; } = new();

        
        public List<SelectListItem> ProjectOptions { get; set; } = new();
        public bool IsEmployee { get; private set; }

        public bool IsManager { get; private set; }
        public bool IsAdmin { get; private set; }

        public async Task OnGetAsync()
        {

            IsAdmin= User.IsInRole(AppRoles.Admin);
            
            IsManager=User.IsInRole(AppRoles.Manager);
            IsEmployee=!IsAdmin && !IsManager;


            
            int? companyId = User.IsInRole(AppRoles.Admin)
                ? null
                : int.Parse(User.FindFirstValue("CompanyId")!);
            //Getting all the projects and employees
            var projectResult = await _projectService.GetAllAsync(companyId);
            if (projectResult.Succeeded)
            {
                Projects = projectResult.Value.ToList();
            }

            var employeeResult = await _employeeService.GetAllAsync(companyId);
            if (employeeResult.Succeeded)
            {
                AllEmployees = employeeResult.Value.ToList();
            }

            var filter = new TaskFilterDto
            {
                CompanyId = companyId,
                PageNumber = 1,
                PageSize = 1000
            };
            
            if (!IsManager && !IsAdmin)
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var result = await _taskService.GetByAssignedUserIdAsync(userId);
                if (!result.Succeeded)
                    return;
                var tasks = result.Value!;

                NotStartedTasks = tasks
                    .Where(t => t.Status == ProjectTasksStatus.NotStarted)
                    .ToList();

                InProgressTasks = tasks
                    .Where(t => t.Status == ProjectTasksStatus.InProgress)
                    .ToList();

                OnHoldTasks = tasks
                    .Where(t => t.Status == ProjectTasksStatus.OnHold)
                    .ToList();

                CompletedTasks = tasks
                    .Where(t => t.Status == ProjectTasksStatus.Completed)
                    .ToList();

                CancelledTasks = tasks
                   .Where(t => t.Status == ProjectTasksStatus.Cancelled)
                   .ToList();

                
            
             }
            else if(IsManager) 
            {
                var result = await _taskService.FilterAsync(filter, companyId);

                if (!result.Succeeded)
                    return;
                var tasks = result.Value!.Items;

                NotStartedTasks = tasks
                    .Where(t => t.Status == ProjectTasksStatus.NotStarted)
                    .ToList();

                InProgressTasks = tasks
                    .Where(t => t.Status == ProjectTasksStatus.InProgress)
                    .ToList();

                OnHoldTasks = tasks
                    .Where(t => t.Status == ProjectTasksStatus.OnHold)
                    .ToList();

                CompletedTasks = tasks
                    .Where(t => t.Status == ProjectTasksStatus.Completed)
                    .ToList();

                CancelledTasks = tasks
                   .Where(t => t.Status == ProjectTasksStatus.Cancelled)
                   .ToList();

                ArchivedTasks = tasks
                   .Where(t => t.Status == ProjectTasksStatus.Archived)
                   .ToList();
            }
            
        }
        public async Task<IActionResult> OnPostCreateTaskAsync()
        {
            int createdByUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            int? actingManagerCompanyId = User.IsInRole(AppRoles.Admin)
                ? null
                : int.Parse(User.FindFirstValue("CompanyId")!);

            
           
                var result = await _taskService.CreateAsync(createTask,createdByUserId,actingManagerCompanyId);  
            
            TempData["SuccessMessage"] ="Project created successfully.";
            return RedirectToPage();
        }

        [HttpPost("Update-TaskStatus")]
        public async Task<IActionResult> OnPostUpdateTaskStatusAsync([FromBody] MoveTaskStatusDto moveTask)
        {
            var actingUserId = int.Parse(
            User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            bool isManager = User.IsInRole(AppRoles.Manager);

            var result = await _taskService.ChangeStatusAsync(moveTask, actingUserId,isManager);
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

        
    }
}

