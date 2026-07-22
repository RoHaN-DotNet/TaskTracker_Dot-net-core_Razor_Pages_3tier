using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Security.Claims;
using TaskTrackerBLL.DTOs.Employee;
using TaskTrackerBLL.DTOs.Task;
using TaskTrackerBLL.Interfaces.Services;
using TaskTrackerDAL.Constants;

namespace TaskTracker.Pages.Tasks
{
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Manager}")]
    public class CreateModel : PageModel
    {
        private readonly ITaskService _taskService;
        private readonly IProjectService _projectService;
        private readonly IEmployeeService _employeeService;

        public CreateModel(
            ITaskService taskService,
            IProjectService projectService,
            IEmployeeService employeeService)
        {
            _taskService = taskService;
            _projectService = projectService;
            _employeeService = employeeService;
        }

        [BindProperty]
        public CreateTaskDto Input { get; set; } = new();

        public MultiSelectList AssigneeOptions { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int projectId)
        {
            Input.ProjectId = projectId;

            var scopeCompanyId = GetScopeCompanyId();
            var projectResult = await _projectService.GetByIdAsync(projectId, scopeCompanyId);

            if (!projectResult.Succeeded)
            {
                return Forbid();
            }

            await PopulateAssigneeOptionsAsync(projectResult.Value!.CompanyId);

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var scopeCompanyId = GetScopeCompanyId();

            if (!ModelState.IsValid)
            {
                var projectForOptions = await _projectService.GetByIdAsync(Input.ProjectId, scopeCompanyId);
                if (projectForOptions.Succeeded)
                {
                    await PopulateAssigneeOptionsAsync(projectForOptions.Value!.CompanyId);
                }
                return Page();
            }

            var createdByUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var result = await _taskService.CreateAsync(Input, createdByUserId, scopeCompanyId);

            if (!result.Succeeded)
            {
                ModelState.AddModelError(string.Empty, result.Error!);
                var projectForOptions = await _projectService.GetByIdAsync(Input.ProjectId, scopeCompanyId);
                if (projectForOptions.Succeeded)
                {
                    await PopulateAssigneeOptionsAsync(projectForOptions.Value!.CompanyId);
                }
                return Page();
            }

            return RedirectToPage("/Projects/Details", new { id = Input.ProjectId });
        }

        private int? GetScopeCompanyId()
        {
            return User.IsInRole(AppRoles.Admin)
                ? null
                : int.Parse(User.FindFirstValue("CompanyId")!);
        }

        private async Task PopulateAssigneeOptionsAsync(int companyId)
        {
            var employeesResult = await _employeeService.SearchAsync(new EmployeeSearchFilterDto(), companyId);

            var employees = employeesResult.Succeeded
                ? employeesResult.Value!
                : new List<EmployeeDto>();

            AssigneeOptions = new MultiSelectList(employees, "Id", "FullName");
        }

    }
}
