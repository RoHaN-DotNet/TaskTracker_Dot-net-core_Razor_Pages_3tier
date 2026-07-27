using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using TaskTrackerBLL.DTOs.Project;
using TaskTrackerBLL.Interfaces.Services;
using TaskTrackerDAL.Constants;

namespace TaskTracker.Pages.Projects
{
    //[Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Manager}")]
    public class AssignMemberModel : PageModel
    {
        private readonly IProjectService _projectService;

        public AssignMemberModel(IProjectService projectService)
        {
            _projectService = projectService;
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

            var result = await _projectService.AddMemberAsync(Input, actingUserId,scopeCompanyId);

            if (!result.Succeeded)
            {
                TempData["ArchiveError"] = result.Error;
            }

            return RedirectToPage("/Projects/Details", new { id = Input.ProjectId });
        }
    }
}
