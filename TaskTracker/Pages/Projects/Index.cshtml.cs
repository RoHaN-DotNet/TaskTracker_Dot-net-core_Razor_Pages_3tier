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
            await LoadPageDataAsync();
        }
        public async Task<IActionResult> OnPostEditAsync()
        {
            

            // Remove unrelated ModelState validation
            // coming from other properties on the page.
            ModelState.Remove("Name");

            Console.WriteLine($"MODELSTATE VALID: {ModelState.IsValid}");

            foreach (var error in ModelState)
            {
                Console.WriteLine(
                    $"KEY: {error.Key} | " +
                    $"ERRORS: {string.Join(", ",
                        error.Value.Errors.Select(e => e.ErrorMessage))}"
                );
            }

            CanManage = User.IsInRole(AppRoles.Manager);
            IsAdmin = User.IsInRole(AppRoles.Admin);

            if (!ModelState.IsValid)
            {
                await LoadPageDataAsync();
                return Page();
            }

            var actingUserId = int.Parse(
                User.FindFirstValue(ClaimTypes.NameIdentifier)!
            );

            var scopeCompanyId = GetScopeCompanyId();

            Console.WriteLine("Calling ProjectService.UpdateAsync...");

            var result = await _projectService.UpdateAsync(
                updateProject,
                actingUserId,
                scopeCompanyId
            );

            Console.WriteLine($"UPDATE RESULT: {result.Succeeded}");
            Console.WriteLine($"UPDATE ERROR: {result.Error}");

            if (!result.Succeeded)
            {
                ModelState.AddModelError(
                    string.Empty,
                    result.Error ?? "Unable to update project."
                );

                await LoadPageDataAsync();
                return Page();
            }

            TempData["SuccessMessage"] =
                "Project updated successfully.";

            return RedirectToPage();
        }

        private int? GetScopeCompanyId()
        {
            if (User.IsInRole(AppRoles.Admin))
            {
                return null;
            }

            var companyIdValue = User.FindFirstValue("CompanyId");

            if (int.TryParse(companyIdValue, out var companyId))
            {
                return companyId;
            }

            return null;
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
            int actingUserId = int.Parse(
                User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            int? scopeCompanyId = GetScopeCompanyId();

            var project = await _projectService.GetByIdAsync(
                updateMember.ProjectId,
                scopeCompanyId);

            if (!project.Succeeded)
            {
                TempData["ErrorMessage"] = project.Error;
                return RedirectToPage();
            }

            var existingMembers = project.Value!.Members
                .Select(m => m.UserId)
                .ToList();

            var selectedMembers = updateMember.MemberUserIds
                .Distinct()
                .ToList();

            var membersToRemove = existingMembers
                .Except(selectedMembers)
                .ToList();

            // Validate all removals before making any changes
            foreach (var userId in membersToRemove)
            {
                var removeResult = await _projectService.RemoveMemberAsync(
                    updateMember.ProjectId,
                    userId,
                    scopeCompanyId);

                if (!removeResult.Succeeded)
                {
                    TempData["ErrorMessage"] = removeResult.Error;
                    return RedirectToPage();
                }
            }

            // Add newly selected members
            foreach (var userId in selectedMembers.Except(existingMembers))
            {
                var addResult = await _projectService.AddMemberAsync(
                    new AssignProjectMemberDto
                    {
                        ProjectId = updateMember.ProjectId,
                        UserId = userId
                    },
                    actingUserId,
                    scopeCompanyId);

                if (!addResult.Succeeded)
                {
                    TempData["ErrorMessage"] = addResult.Error;
                    return RedirectToPage();
                }
            }

            TempData["SuccessMessage"] =
                "Project members updated successfully.";

            return RedirectToPage();
        }
        private async Task PopulateOptionsAsync()
        {
            int companyId = int.Parse(
                User.FindFirstValue("CompanyId")!);

            createProject.CompanyId = companyId;

            var employeesResult = await _employeeService.SearchAsync(
                new EmployeeSearchFilterDto(),
                companyId);

            AllEmployees = employeesResult.Succeeded
                ? employeesResult.Value!
                : new List<EmployeeDto>();
        }
        private async Task LoadPageDataAsync()
        {
            CanManage = User.IsInRole(AppRoles.Manager);
            IsAdmin = User.IsInRole(AppRoles.Admin);

            if (!CanManage && !IsAdmin)
            {
                IsEmployeeView = true;

                var userId = int.Parse(
                    User.FindFirstValue(ClaimTypes.NameIdentifier)!
                );

                var result = await _projectService.GetByMemberUserIdAsync(userId);

                AssignedProjects = result.Succeeded
                    ? result.Value!
                    : Array.Empty<ProjectDto>();

                return;
            }

            int? scopeCompanyId = User.IsInRole(AppRoles.Admin)
                ? null
                : int.Parse(User.FindFirstValue("CompanyId")!);

            var searchResult = await _projectService.SearchAsync(
                Filter,
                scopeCompanyId
            );

            PagedProjects = searchResult.Succeeded
                ? searchResult.Value
                : null;

            if (CanManage)
            {
                await PopulateOptionsAsync();
            }
        }
        private async Task ReloadPageDataAsync()
        {
            int? scopeCompanyId = User.IsInRole(AppRoles.Admin)
                ? null
                : int.Parse(User.FindFirstValue("CompanyId")!);

            var searchResult = await _projectService.SearchAsync(
                Filter,
                scopeCompanyId);

            PagedProjects = searchResult.Succeeded
                ? searchResult.Value
                : null;

            if (CanManage)
            {
                await PopulateOptionsAsync();
            }
        }
    }
}


