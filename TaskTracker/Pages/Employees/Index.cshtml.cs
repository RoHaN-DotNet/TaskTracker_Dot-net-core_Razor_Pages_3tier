using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Security.Claims;
using TaskTrackerBLL.Common;
using TaskTrackerBLL.DTOs.Employee;
using TaskTrackerBLL.DTOs.Role;
using TaskTrackerBLL.Interfaces;
using TaskTrackerBLL.Interfaces.Services;
using TaskTrackerDAL.Constants;

namespace TaskTracker.Pages.Employee
{
    public class IndexModel : PageModel
    {
        private readonly IEmployeeService _employeeService;
        private readonly IRoleService _roleService;
        public IndexModel(IEmployeeService employeeService,IRoleService roleService)
        {
            _employeeService = employeeService;
            _roleService = roleService;
        }
        [BindProperty(SupportsGet = true)]
        public EmployeeSearchFilterDto Filter { get; set; } = new();

        public IReadOnlyList<EmployeeDto>? Employees { get; set; }=Array.Empty<EmployeeDto>();
        public IReadOnlyList<string> RoleOptions { get; } = AppRoles.EmployeeRoles;
        [BindProperty]
        public EditEmployeeDto EditEmployee { get; set; } = new();
        public List<SelectListItem> Role { get; set; }=new();


        public async Task OnGetAsync()
        {
            
           /* int? scopeCompanyId = User.IsInRole(AppRoles.Admin)
           ? null
           : int.Parse(User.FindFirstValue("CompanyId")!);*/
           
            var result = await _employeeService.SearchAsync(Filter, null);//scopeCompanyId);

            Employees = result.Succeeded ? result.Value! : Array.Empty<EmployeeDto>();

            var roleResult = await _roleService.GetAllAsync();
            if (roleResult.Succeeded) 
            { 
                Role=roleResult.Value!
                    .Where(r=>r.Name!=AppRoles.Admin)
                    .Select(r=>new SelectListItem
                    {
                        Value=r.Name,
                        Text=r.Description,
                    } ).ToList();
            }
        }

        public async Task<IActionResult> OnPostUpdateAsync()
        {
            var result=await _employeeService.UpdateAsync(EditEmployee,null);
            if (!result.Succeeded)
            {
                TempData["result"]=result.Errors;
                return Page();
            }
            return RedirectToPage();

        }
        public async Task<IActionResult> OnPostChangeStatusAsync(int id,bool isActive)
        {
            TempData["Debug"] = $"id={id}, isActive={isActive}";

            /*
        int? actingManagerCompanyId = User.IsInRole(AppRoles.Admin)
        ? null
        : int.Parse(User.FindFirstValue("CompanyId")!);*/
            var result = await _employeeService.DisableAsync(id,isActive);//actingManagerCompanyId);

            if (!result.Succeeded)
            {
                TempData["Error"] = result.Error;
            }

            return RedirectToPage();
        }


    }
}
