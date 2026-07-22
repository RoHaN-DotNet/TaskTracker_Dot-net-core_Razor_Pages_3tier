using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using TaskTrackerBLL.DTOs.Employee;
using TaskTrackerBLL.DTOs.Project;
using TaskTrackerBLL.Interfaces.Services;
using TaskTrackerDAL.Constants;

namespace TaskTracker.Pages.Projects
{
    public class DetailsModel : PageModel
    {
        private readonly IProjectService _projectService;
        private readonly IEmployeeService _employeeService;
        private readonly IAuthorizationService _authorizationService;

        public DetailsModel(
            IProjectService projectService,
            IEmployeeService employeeService,
            IAuthorizationService authorizationService)
        {
            _projectService = projectService;
            _employeeService = employeeService;
            _authorizationService = authorizationService;
        }

        public ProjectDto Project { get; set; } = default!;

        public bool CanManage { get; set; }

        public IReadOnlyList<EmployeeDto> AssignableEmployees { get; set; } = Array.Empty<EmployeeDto>();

        public async Task<IActionResult> OnGetAsync(int id)
        {
            CanManage = User.IsInRole(AppRoles.Admin) || User.IsInRole(AppRoles.Manager);

            int? projectResultScope = User.IsInRole(AppRoles.Admin)
                ? null
                : (User.IsInRole(AppRoles.Manager) ? int.Parse(User.FindFirstValue("CompanyId")!) : null);

            var result = await _projectService.GetByIdAsync(id, projectResultScope);

            if (!result.Succeeded)
            {
                if (!CanManage)
                {
                    // Employee tier: fall back to a resource-based membership check
                    // instead of the company-scoped one above, which doesn't apply to them.
                    var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                    var membershipResult = await _projectService.GetByMemberUserIdAsync(userId);

                    var owned = membershipResult.Succeeded
                        ? membershipResult.Value!.FirstOrDefault(p => p.Id == id)
                        : null;

                    if (owned is null)
                    {
                        return Forbid();
                    }

                    Project = owned;
                    return Page();
                }

                return Forbid();
            }

            Project = result.Value!;

            if (CanManage)
            {
                var employeesResult = await _employeeService.SearchAsync(
                    new EmployeeSearchFilterDto(), Project.CompanyId);

                AssignableEmployees = employeesResult.Succeeded
                    ? employeesResult.Value!
                    : Array.Empty<EmployeeDto>();
            }

            return Page();
        }
        [BindProperty]
        public AssignProjectMemberDto Input { get; set; } = new();

        public async Task<IActionResult> OnPostAsync()
        {
            var actingUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var scopeCompanyId = User.IsInRole(AppRoles.Admin)
                ? null
                : (int?)int.Parse(User.FindFirstValue("CompanyId")!);

            if (!ModelState.IsValid)
            {
                TempData["ArchiveError"] = "Please select an employee to assign.";
                return RedirectToPage("/Projects/Details", new { id = Input.ProjectId });
            }

            var result = await _projectService.AddMemberAsync(Input, actingUserId, scopeCompanyId);

            if (!result.Succeeded)
            {
                TempData["ArchiveError"] = result.Error;
            }

            return RedirectToPage("/Projects/Details", new { id = Input.ProjectId });
        }

    }
}
