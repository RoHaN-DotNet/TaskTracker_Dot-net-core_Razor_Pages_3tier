using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Security.Claims;
using TaskTrackerBLL.Common;
using TaskTrackerBLL.DTOs.Employee;
using TaskTrackerBLL.DTOs.Project;
using TaskTrackerBLL.Interfaces.Services;
using TaskTrackerDAL.Constants;
using TaskTrackerDAL.Models;
using TaskTrackerDAL.Models.Enums;

namespace TaskTracker.Pages.Projects
{
    public class IndexModel : PageModel
    {
        private readonly IProjectService _projectService;
        private readonly ICompanyService _companyService;
        private readonly IEmployeeService _employeeService;
        public IndexModel(IProjectService projectService,ICompanyService companyService, IEmployeeService employeeService)
        {
            _projectService = projectService;
            _companyService = companyService;
            _employeeService=employeeService;
        }

        [BindProperty(SupportsGet = true)]
        public ProjectSearchFilterDto Filter { get; set; } = new();

        public PagedResult<ProjectDto>? PagedProjects { get; set; }

        public IReadOnlyList<ProjectDto> AssignedProjects { get; set; } = Array.Empty<ProjectDto>();

        public bool IsEmployeeView { get; private set; }

        public bool CanManage { get; private set; }
        public bool IsAdmin { get; private set; }

        [BindProperty]
        public UpdateProjectDto updateProject { get; set; } = new();
        public List<SelectListItem> StatusOptions { get; } =Enum.GetValues<ProjectStatus>()
            .Select(status => new SelectListItem
            {
                Value = ((int)status).ToString(),
                Text = status.ToString()
            }).ToList();
        [BindProperty]
        public CreateProjectDto createProject { get; set; } = new();

        public MultiSelectList MemberOptions { get; set; } = new(Array.Empty<object>());
        [BindProperty]
        public UpdateProjectMemberDto updateMember { get; set; }
        public IReadOnlyList<EmployeeDto> AllEmployees { get; set; } = new List<EmployeeDto>();
        
        public async Task OnGetAsync()
        {
            CanManage = User.IsInRole(AppRoles.Manager);
            IsAdmin=User.IsInRole(AppRoles.Admin);

            if (!CanManage && !IsAdmin)
            {
                IsEmployeeView = true;
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var result = await _projectService.GetByMemberUserIdAsync(userId);
                AssignedProjects = result.Succeeded ? result.Value! : Array.Empty<ProjectDto>();
                return;
            }

            int? scopeCompanyId = User.IsInRole(AppRoles.Admin)
                ? null
                : int.Parse(User.FindFirstValue("CompanyId")!);

            var searchResult = await _projectService.SearchAsync(Filter, scopeCompanyId);
            PagedProjects = searchResult.Succeeded ? searchResult.Value : null;

            if (CanManage)
            {
                await PopulateOptionsAsync();
            }

        }
        public async Task<IActionResult> OnPostEditAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var actingUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var scopeCompanyId = GetScopeCompanyId();

            var result = await _projectService.UpdateAsync(updateProject, actingUserId, scopeCompanyId);

            if (!result.Succeeded)
            {
                ModelState.AddModelError(string.Empty, result.Error!);
                return Page();
            }
            return RedirectToPage();
            //return RedirectToPage("/Projects/Details", new { id = updateProject.Id });
        }

        private int? GetScopeCompanyId()
        {
            return User.IsInRole(AppRoles.Admin)
                ? null
                : int.Parse(User.FindFirstValue("CompanyId")!);
        }
        public async Task<IActionResult> OnPostCreateAsync()
        {
            
            var scopeCompanyId = GetScopeCompanyId();
            createProject.CompanyId = scopeCompanyId ?? 0;
            //if (!ModelState.IsValid)
            //{
            //    await PopulateOptionsAsync();
            //    return Page();
            //}

            var createdByUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var result = await _projectService.CreateAsync(createProject, createdByUserId, scopeCompanyId);

            if (!result.Succeeded)
            {
                ModelState.AddModelError(string.Empty, result.Error!);
                await PopulateOptionsAsync();
                return Page();
            }
            TempData["SuccessMessage"] =
            "Project created successfully.";

            return RedirectToPage();
        }
        public async Task<IActionResult> OnPostAddOrRemoveMemberAsync()
        {
            int actingUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            int? scopeCompanyId = GetScopeCompanyId();

            // Current members in database
            var project = await _projectService.GetByIdAsync(updateMember.ProjectId, scopeCompanyId);
            /*
            if (!project.Succeeded)
            {
                TempData["ErrorMessage"] = project.Error;
                return RedirectToPage();
            }*/

            var existingMembers = project.Value!.Members.Select(m => m.UserId).ToList();

            // Add newly selected members
            foreach (var userId in updateMember.MemberUserIds.Except(existingMembers))
            {
                await _projectService.AddMemberAsync(
                    new AssignProjectMemberDto
                    {
                        ProjectId = updateMember.ProjectId,
                        UserId = userId
                    },
                    actingUserId,
                    scopeCompanyId);
            }

            // Remove unchecked members
            foreach (var userId in existingMembers.Except(updateMember.MemberUserIds))
            {
                await _projectService.RemoveMemberAsync(
                    updateMember.ProjectId,
                    userId,
                    scopeCompanyId);
            }

            TempData["SuccessMessage"] = "Project members updated successfully.";

            return RedirectToPage();
        }
        private async Task PopulateOptionsAsync()
        {
            int companyId = int.Parse(User.FindFirstValue("CompanyId")!);
            createProject.CompanyId = companyId;

            var employees = await _employeeService.SearchAsync(
                  new EmployeeSearchFilterDto(),
                  companyId);

            AllEmployees = employees.Value!;
            var employeesResult = await _employeeService.SearchAsync(
        new EmployeeSearchFilterDto(),
        companyId);

            AllEmployees = employeesResult.Succeeded
                ? employeesResult.Value!
                : new List<EmployeeDto>();

        }
    }
}


