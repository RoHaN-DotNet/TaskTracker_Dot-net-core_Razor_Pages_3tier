using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using TaskTrackerBLL.DTOs.Employee;
using TaskTrackerBLL.Interfaces.Services;
using TaskTrackerDAL.Constants;

namespace TaskTracker.Pages.Employee
{
    public class DetailsModel : PageModel
    {
        private readonly IEmployeeService _employeeService;

        public DetailsModel(IEmployeeService employeeService)
        {
            _employeeService = employeeService;
        }

        public EmployeeDto Employee { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var scopeCompanyId = GetScopeCompanyId();

            var result = await _employeeService.GetByIdAsync(id, scopeCompanyId);

            if (!result.Succeeded)
            {
                return Forbid();
            }

            Employee = result.Value!;

            return Page();
        }

        /*public async Task<IActionResult> OnPostDisableAsync(int id)
        {
            var scopeCompanyId = GetScopeCompanyId();

           var result = await _employeeService.DisableAsync(id, scopeCompanyId);

            if (!result.Succeeded)
            {
                ModelState.AddModelError(string.Empty, result.Error!);

                var employeeResult = await _employeeService.GetByIdAsync(id, scopeCompanyId);
                Employee = employeeResult.Value!;

                return Page();
            }

            return RedirectToPage("/Employees/Index");
        }*/

        private int? GetScopeCompanyId()
        {
            return User.IsInRole(AppRoles.Admin)
                ? null
                : int.Parse(User.FindFirstValue("CompanyId")!);
        }
    }
}
