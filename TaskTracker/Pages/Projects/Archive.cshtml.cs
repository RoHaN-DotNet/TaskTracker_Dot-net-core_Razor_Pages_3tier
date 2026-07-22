using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using TaskTrackerBLL.Interfaces.Services;
using TaskTrackerDAL.Constants;

namespace TaskTracker.Pages.Projects
{
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Manager}")]
    public class ArchiveModel : PageModel
    {
        private readonly IProjectService _projectService;

        public ArchiveModel(IProjectService projectService)
        {
            _projectService = projectService;
        }

        public async Task<IActionResult> OnPostAsync(int id)
        {
            var actingUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var scopeCompanyId = User.IsInRole(AppRoles.Admin)
                ? null
                : (int?)int.Parse(User.FindFirstValue("CompanyId")!);

            var result = await _projectService.ArchiveAsync(id, actingUserId, scopeCompanyId);

            if (!result.Succeeded)
            {
                TempData["ArchiveError"] = result.Error;
            }

            return RedirectToPage("/Projects/Details", new { id });
        }
    }
}
