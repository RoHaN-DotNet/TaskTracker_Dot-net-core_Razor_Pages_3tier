using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Rewrite;
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
        private readonly ICompanyService _companyService;

        public IndexModel(IEmployeeService employeeService,IRoleService roleService, ICompanyService companyService)
        {
            _employeeService = employeeService;
            _roleService = roleService;
            _companyService = companyService;
        }
        [BindProperty(SupportsGet = true)]
        public EmployeeSearchFilterDto Filter { get; set; } = new();

        public IReadOnlyList<EmployeeDto>? Employees { get; set; }=Array.Empty<EmployeeDto>();
        public IReadOnlyList<string> RoleOptions { get; } = AppRoles.EmployeeRoles;
        [BindProperty]
        public EditEmployeeDto EditEmployee { get; set; } = new();
        [BindProperty]
        public SignupEmployeeDto SignupEmployee { get; set; } = new();
        public List<SelectListItem> Role { get; set; }=new();

        public List<SelectListItem> Companies { get; set; } = new();
        private async Task LoadDropdownsAsync()
        {
            var roleResult = await _roleService.GetAllAsync();

            if (roleResult.Succeeded)
            {
                Role = roleResult.Value!
                    .Where(r => r.Name != AppRoles.Admin)
                    .Select(r => new SelectListItem
                    {
                        Value = r.Name,
                        Text = r.Description
                    })
                    .ToList();
            }

            var companies = await _companyService.GetAllAsync();

            if (companies.Succeeded)
            {
                Companies = companies.Value
                    .Select(c => new SelectListItem
                    {
                        Value = c.Id.ToString(),
                        Text = c.Name
                    })
                    .ToList();
            }
        }

        //get all employee info
        public async Task OnGetAsync()
        {
           /* 
            * int? scopeCompanyId = User.IsInRole(AppRoles.Admin)
           ? null
           : int.Parse(User.FindFirstValue("CompanyId")!);*/
            var result = await _employeeService.SearchAsync(Filter, null);//scopeCompanyId);

            Employees = result.Succeeded ? result.Value! : Array.Empty<EmployeeDto>();
            await LoadDropdownsAsync();
            /*
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
            var companies = await _companyService.GetAllAsync();
            if (companies.Succeeded)
            {

                Companies = companies.Value.Select(
                c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.Name

                }).ToList();
            }*/
        }
        //Update employee name ,email,role
        public async Task<IActionResult> OnPostUpdateAsync()
        {
            ModelState.Clear();
            if (!TryValidateModel(EditEmployee, nameof(EditEmployee)))
            {
                TempData["ErrorMessage"] = string.Join("</br>",
                    ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage));
                return RedirectToPage();
            }
            var result=await _employeeService.UpdateAsync(EditEmployee,null);
            if (!result.Succeeded)
            {
                TempData["ErrorMessage"] = result.Errors is { Count: > 0 }
            ? string.Join("<br/>", result.Errors)
            : result.Error;

                return RedirectToPage();
            }
            TempData["SuccessMessage"] = "Employee updated successfully.";
            return RedirectToPage();

        }
        //Status change
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
                TempData["ErrorMessage"] = result.Error;
            }
            else
            {
                TempData["SuccessMessage"] = isActive ? "Employee activated." : "Employee deactivated.";
            }

            return RedirectToPage();
        }
        public async Task<IActionResult> OnPostCreateAsync()
        {
            ModelState.Clear();

            if (!TryValidateModel(SignupEmployee, nameof(SignupEmployee)))
            {
                TempData["ErrorMessage"] = string.Join("<br/>",
                   ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage));
                /*
                await LoadDropdownsAsync();

                var employees = await _employeeService.SearchAsync(Filter, null);
                Employees = employees.Succeeded ? employees.Value! : Array.Empty<EmployeeDto>();*/

                return RedirectToPage();
            }

            var result = await _employeeService.RegisterAsync(SignupEmployee, null);

            if (!result.Succeeded)
            {

                /*
                 * ModelState.AddModelError(string.Empty, result.Error!);
                await LoadDropdownsAsync();*/

                /*
                 * var employees = await _employeeService.SearchAsync(Filter, null);
                Employees = employees.Succeeded ? employees.Value! : Array.Empty<EmployeeDto>();*/

                TempData["ErrorMessage"] = result.Error;
                return RedirectToPage();
            }
            TempData["SuccessMessage"] = $"Employee '{SignupEmployee.FullName}' was created successfully.";
            return RedirectToPage();
        }
        

    }
}
