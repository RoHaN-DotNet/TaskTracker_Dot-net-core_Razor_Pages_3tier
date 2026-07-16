using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Security.Claims;
using TaskTrackerBLL.DTOs.Project;
using TaskTrackerBLL.Interfaces.Services;
using TaskTrackerDAL.Constants;
using static System.Net.Mime.MediaTypeNames;

namespace TaskTracker.Pages.Projects
{
    public class CreateModel : PageModel
    {
        private readonly IProjectService _projectService;
        private readonly ICompanyService _companyService;
        private readonly IEmployeeService _employeeService;

        public CreateModel(
            IProjectService projectService,
            ICompanyService companyService,
            IEmployeeService employeeService)
        {
            _projectService = projectService;
            _companyService = companyService;
            _employeeService = employeeService;
        }

        [BindProperty]
        public CreateProjectDto Input { get; set; } = new();

        public SelectList CompanyOptions { get; set; } = default!;

        public MultiSelectList MemberOptions { get; set; } = default!;

        public bool IsAdmin { get; set; }

        public async Task OnGetAsync()
        {
            IsAdmin = User.IsInRole(AppRoles.Admin);

            if (!IsAdmin)
            {
                Input.CompanyId = int.Parse(User.FindFirstValue("CompanyId")!);
            }

            await PopulateOptionsAsync();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var scopeCompanyId = GetScopeCompanyId();

            if (!ModelState.IsValid)
            {
                await PopulateOptionsAsync();
                return Page();
            }

            var createdByUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var result = await _projectService.CreateAsync(Input, createdByUserId, scopeCompanyId);

            if (!result.Succeeded)
            {
                ModelState.AddModelError(string.Empty, result.Error!);
                await PopulateOptionsAsync();
                return Page();
            }

            return RedirectToPage("/Projects/Details", new { id = result.Value!.Id });
        }

        private int? GetScopeCompanyId()
        {
            return User.IsInRole(AppRoles.Admin)
                ? null
                : int.Parse(User.FindFirstValue("CompanyId")!);
        }

        private async Task PopulateOptionsAsync()
        {
            IsAdmin = User.IsInRole(AppRoles.Admin);

            if (IsAdmin)
            {
                var companiesResult = await _companyService.GetAllAsync();
                var companies = companiesResult.Succeeded
                    ? companiesResult.Value!
                    : new List<TaskTrackerBLL.DTOs.Company.CompanyDto>();
                CompanyOptions = new SelectList(companies, "Id", "Name");
            }

            var effectiveCompanyId = Input.CompanyId != 0
                ? Input.CompanyId
                : GetScopeCompanyId() ?? 0;

            if (effectiveCompanyId != 0)
            {
                var employeesResult = await _employeeService.SearchAsync(
                    new TaskTrackerBLL.DTOs.Employee.EmployeeSearchFilterDto(), effectiveCompanyId);

                var employees = employeesResult.Succeeded
                    ? employeesResult.Value!
                    : new List<TaskTrackerBLL.DTOs.Employee.EmployeeDto>();

                MemberOptions = new MultiSelectList(employees, "Id", "FullName");
            }
            else
            {
                MemberOptions = new MultiSelectList(Array.Empty<object>());
            }
        }
    }
}
