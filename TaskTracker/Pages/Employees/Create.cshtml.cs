using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using TaskTrackerBLL.DTOs.Employee;
using TaskTrackerBLL.Interfaces.Services;
using TaskTrackerDAL.Constants;

namespace TaskTracker.Pages.Employee
{
    public class CreateModel : PageModel
    {
        private readonly IEmployeeService _employeeService;

        public CreateModel(IEmployeeService employeeService)
        {
            _employeeService = employeeService;
        }

        [BindProperty]
        public SignupEmployeeDto Input { get; set; } = new();

        public IReadOnlyList<string> RoleOptions { get; } = AppRoles.EmployeeRoles;

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var actingCompanyId = int.Parse(User.FindFirstValue("CompanyId")!);

            var result = await _employeeService.RegisterAsync(Input, actingCompanyId);

            if (!result.Succeeded)
            {
                ModelState.AddModelError(string.Empty, result.Error!);
                return Page();
            }

            return RedirectToPage("/Employees/Index");
        }
    }
}
