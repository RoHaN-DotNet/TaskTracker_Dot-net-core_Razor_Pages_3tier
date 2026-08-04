using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Security.Claims;
using TaskTrackerBLL.DTOs.Employee;
using TaskTrackerBLL.DTOs.Tasks;
using TaskTrackerBLL.Interfaces.Services;
using TaskTrackerDAL.Constants;

namespace TaskTracker.Pages.Tasks
{
    //[Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Manager}")]
    public class AssignModel : PageModel
    {
        private readonly ITaskService _taskService;
        private readonly IProjectService _projectService;
        private readonly IEmployeeService _employeeService;

        public AssignModel(
            ITaskService taskService, IProjectService projectService, IEmployeeService employeeService)
        {
            _taskService = taskService;
            _projectService = projectService;
            _employeeService = employeeService;
        }

        [BindProperty]
        public AssignTaskDto Input { get; set; } = new();

        public string TaskTitle { get; set; } = string.Empty;

        public SelectList EmployeeOptions { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var taskResult = await _taskService.GetByIdAsync(id);
            if (!taskResult.Succeeded)
            {
                return NotFound();
            }

            Input.TaskId = id;
            TaskTitle = taskResult.Value!.Title;

            await PopulateEmployeeOptionsAsync(taskResult.Value!.ProjectId);

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var scopeCompanyId = GetScopeCompanyId();

            if (!ModelState.IsValid)
            {
                return Page();
            }

            var result = await _taskService.AssignAsync(Input, scopeCompanyId);

            if (!result.Succeeded)
            {
                ModelState.AddModelError(string.Empty, result.Error!);
                var taskResult = await _taskService.GetByIdAsync(Input.TaskId);
                if (taskResult.Succeeded)
                {
                    TaskTitle = taskResult.Value!.Title;
                    await PopulateEmployeeOptionsAsync(taskResult.Value!.ProjectId);
                }
                return Page();
            }

            return RedirectToPage("/Tasks/Details", new { id = Input.TaskId });
        }

        private int? GetScopeCompanyId()
        {
            return User.IsInRole(AppRoles.Admin)
                ? null
                : int.Parse(User.FindFirstValue("CompanyId")!);
        }

        private async Task PopulateEmployeeOptionsAsync(int projectId)
        {
            var projectResult = await _projectService.GetByIdAsync(projectId, null);
            var companyId = projectResult.Succeeded ? projectResult.Value!.CompanyId : 0;

            var employeesResult = await _employeeService.SearchAsync(new EmployeeSearchFilterDto(), companyId);
            var employees = employeesResult.Succeeded ? employeesResult.Value! : new List<EmployeeDto>();

            EmployeeOptions = new SelectList(employees, "Id", "FullName");
        }
    }
}
