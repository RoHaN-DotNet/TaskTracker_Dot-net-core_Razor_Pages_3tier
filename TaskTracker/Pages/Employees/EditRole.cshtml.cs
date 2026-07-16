using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;
using TaskTrackerDAL.Constants;
using TaskTrackerBLL.DTOs.Employee;
using TaskTrackerBLL.DTOs.Role;
using TaskTrackerBLL.Interfaces.Services;
using TaskTrackerBLL.Services;
using System.Security.Claims;

namespace TaskTracker.Pages.Employees
{
    public class EditRoleModel : PageModel
    {
        private readonly IEmployeeService _employeeService;
        private readonly IRoleAssignmentService _roleAssignmentService;

        public EditRoleModel(
            IEmployeeService employeeService,
            IRoleAssignmentService roleAssignmentService)
        {
            _employeeService= employeeService;
            _roleAssignmentService = roleAssignmentService;
        }
        [BindProperty]
        public UpdateEmployeeRoleDto Input { get; set; } = new();

        public string EmployeeName { get; set; } = string.Empty;

        public SelectList RoleOptions { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var scopeCompanyId = GetScopeCompanyId();

            var employeeResult = await _employeeService.GetByIdAsync(id, scopeCompanyId);

            if (!employeeResult.Succeeded)
            {
                return Forbid();
            }
            EmployeeName = employeeResult.Value!.FullName;
            Input.EmployeeId = id;

            await PopulateRoleOptionsAsync();

            return Page();
        }
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await PopulateRoleOptionsAsync();
                return Page();
            }
            var isAdmin = User.IsInRole(AppRoles.Admin);
            var scopeCompanyId = GetScopeCompanyId();

            var result = await _roleAssignmentService.AssignRoleAsync(Input, isAdmin, scopeCompanyId);

            if (!result.Succeeded)
            {
                ModelState.AddModelError(string.Empty, result.Error!);

                var employeeResult = await _employeeService.GetByIdAsync(Input.EmployeeId, scopeCompanyId);
                EmployeeName = employeeResult.Succeeded ? employeeResult.Value!.FullName : string.Empty;

                await PopulateRoleOptionsAsync();
                return Page();
            }
            return RedirectToPage("/Employees/Details", new { id = Input.EmployeeId });
        }

        private async Task PopulateRoleOptionsAsync()
        {
            var isAdmin = User.IsInRole(AppRoles.Admin);
            var rolesResult = await _roleAssignmentService.GetAssignableRolesAsync(isAdmin);

            var roles = rolesResult.Succeeded
                ? rolesResult.Value!
                : new List<AssignableRoleDto>();
            RoleOptions = new SelectList(roles, nameof(AssignableRoleDto.Name), nameof(AssignableRoleDto.Name));
        }
        private int? GetScopeCompanyId()
        {
           return User.IsInRole(AppRoles.Admin)
                ? null
                : int.Parse(User.FindFirstValue("CompanyId")!);

        }

        
     
    
    }
}
