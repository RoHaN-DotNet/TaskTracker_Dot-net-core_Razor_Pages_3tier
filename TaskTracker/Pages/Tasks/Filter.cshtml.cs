using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Security.Claims;
using TaskTrackerBLL.Common;
using TaskTrackerBLL.DTOs.Employee;

using TaskTrackerBLL.DTOs.Tasks;
using TaskTrackerBLL.Interfaces.Services;
using TaskTrackerDAL.Constants;

namespace TaskTracker.Pages.Tasks
{
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Manager}")]
    public class FilterModel : PageModel
    {
        private readonly ITaskService _taskService;
        private readonly IEmployeeService _employeeService;

        public FilterModel(ITaskService taskService, IEmployeeService employeeService)
        {
            _taskService = taskService;
            _employeeService = employeeService;
        }
        [BindProperty(SupportsGet = true)]
        public TaskFilterDto Filter { get; set; } = new();
        public PagedResult<TaskDto>? Result { get; set; }

        public SelectList EmployeeOptions { get; set; } = default!;

        public async Task OnGetAsync()
        {
            var isAdmin=User.IsInRole(AppRoles.Admin);
            int? scopeCompanyId = isAdmin ? null : int.Parse(User.FindFirstValue("CompanyId")!);

            var employeeScope = Filter.CompanyId ?? scopeCompanyId;
            var employeesResult = await _employeeService.SearchAsync(new EmployeeSearchFilterDto(), employeeScope);
            var employees = employeesResult.Succeeded ? employeesResult.Value! : new List<EmployeeDto>();
            EmployeeOptions = new SelectList(employees, "Id", "FullName");

            var result = await _taskService.FilterAsync(Filter, scopeCompanyId);
            Result = result.Succeeded ? result.Value : null;

        }
    }
}
