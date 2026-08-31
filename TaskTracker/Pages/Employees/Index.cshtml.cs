using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Rewrite;
using System.Security.Claims;
using TaskTrackerBLL.Common;
using TaskTrackerBLL.DTOs.Employee;
using TaskTrackerBLL.DTOs.Role;
using TaskTrackerBLL.Infrastucture;
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
        private readonly IUnitOfWork _unitOfWork;

        public IndexModel(IEmployeeService employeeService,IRoleService roleService, ICompanyService companyService, IUnitOfWork unitOfWork)
        {
            _employeeService = employeeService;
            _roleService = roleService;
            _companyService = companyService;
            _unitOfWork = unitOfWork;
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
        public int? ActingUserCompanyId { get; set; }
        private async Task LoadDropdownsAsync()
        {
            var roleResult = await _roleService.GetAllAsync();

            if (roleResult.Succeeded)
            {
                Role = roleResult.Value!
                    .Where(r =>
                        r.Name != AppRoles.Admin &&
                        r.Name != AppRoles.Manager)
                    .Select(r => new SelectListItem
                    {
                        Value = r.Name,
                        Text = r.Name
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
        public bool IsManager;
        //get all employee info
        public async Task<IActionResult> OnGetAsync()
        {
             IsManager = User.IsInRole(AppRoles.Manager);

            var userIdClaim =
                User.FindFirst(ClaimTypes.NameIdentifier);

            if (IsManager)
            {
                if (userIdClaim == null ||
                    !int.TryParse(userIdClaim.Value, out int userId))
                {
                    return Forbid();
                }

                var currentUser =
                    await _unitOfWork.Users.GetByIdAsync(userId);

                if (currentUser == null)
                {
                    return Forbid();
                }

                ActingUserCompanyId = currentUser.CompanyId;
            }

            // তোমার existing dropdown/load code এখানে থাকবে
            await LoadDropdownsAsync();

            var employees =
                await _employeeService.SearchAsync(
                    Filter,
                    ActingUserCompanyId,
                    IsManager
                        ? int.Parse(userIdClaim!.Value)
                        : null);

            Employees = employees.Succeeded
                ? employees.Value!
                : Array.Empty<EmployeeDto>();

            return Page();
        }
        //Update employee name ,email,role
        public async Task<IActionResult> OnPostUpdateAsync()
        {
            ModelState.Clear();
            if (!TryValidateModel(EditEmployee, nameof(EditEmployee)))
            {
                TempData["ErrorMessage"] = string.Join("</br>",ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
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

             IsManager = User.IsInRole(AppRoles.Manager);

            int? actingUserCompanyId = null;

            // -------------------------------------------------
            // MANAGER
            // -------------------------------------------------
            if (IsManager)
            {
                var userIdClaim =
                    User.FindFirst(ClaimTypes.NameIdentifier);

                if (userIdClaim == null ||
                    !int.TryParse(userIdClaim.Value, out int userId))
                {
                    TempData["ErrorMessage"] =
                        "Unable to identify the logged-in user.";

                    return RedirectToPage();
                }

                var currentUser =
                    await _unitOfWork.Users.GetByIdAsync(userId);

                if (currentUser == null)
                {
                    TempData["ErrorMessage"] =
                        "Your user account could not be found.";

                    return RedirectToPage();
                }

                // Manager's company
                actingUserCompanyId = currentUser.CompanyId;

                // IMPORTANT:
                // We set this only for server-side validation.
                // Manager does NOT select it from the UI.
                SignupEmployee.CompanyId =
                    currentUser.CompanyId;
            }

            // -------------------------------------------------
            // VALIDATION
            // -------------------------------------------------
            if (!TryValidateModel(
                    SignupEmployee,
                    nameof(SignupEmployee)))
            {
                TempData["ErrorMessage"] =
                    string.Join(
                        "<br/>",
                        ModelState.Values
                            .SelectMany(v => v.Errors)
                            .Select(e => e.ErrorMessage));

                return RedirectToPage();
            }

            // -------------------------------------------------
            // CREATE EMPLOYEE
            // -------------------------------------------------
            var result =
                await _employeeService.RegisterAsync(
                    SignupEmployee,
                    actingUserCompanyId);

            if (!result.Succeeded)
            {
                TempData["ErrorMessage"] =
                    result.Error;

                return RedirectToPage();
            }

            TempData["SuccessMessage"] =
                $"Employee '{SignupEmployee.FullName}' was created successfully.";

            return RedirectToPage();
        }


    }
}
