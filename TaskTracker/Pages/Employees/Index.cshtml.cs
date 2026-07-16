using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using TaskTrackerBLL.DTOs.Employee;
using TaskTrackerBLL.Interfaces.Services;
using TaskTrackerDAL.Constants;

namespace TaskTracker.Pages.Employee
{
    public class IndexModel : PageModel
    {
        private readonly IEmployeeService _employeeService;

        public IndexModel(IEmployeeService employeeService)
        {
            _employeeService = employeeService;
        }

        [BindProperty(SupportsGet = true)]
        public EmployeeSearchFilterDto Filter { get; set; } = new();

        public IReadOnlyList<EmployeeDto> Employees { get; set; } = Array.Empty<EmployeeDto>();

        public IReadOnlyList<string> RoleOptions { get; } = AppRoles.EmployeeRoles;

        public async Task OnGetAsync()
        {
            int? scopeCompanyId = User.IsInRole(AppRoles.Admin)
                ? null
                : int.Parse(User.FindFirstValue("CompanyId")!);

            var result = await _employeeService.SearchAsync(Filter, scopeCompanyId);

            Employees = result.Succeeded ? result.Value! : Array.Empty<EmployeeDto>();
        }
    }
}
