using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using TaskTrackerBLL.DTOs.Project;
using TaskTrackerBLL.Interfaces.Services;
using TaskTrackerDAL.Constants;

namespace TaskTracker.Pages.Projects
{
    public class EditModel : PageModel
    {
        private readonly IProjectService _projectService;

        public EditModel(IProjectService projectService)
        {
            _projectService = projectService;
        }

        [BindProperty]
        public UpdateProjectDto Input { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var scopeCompanyId = GetScopeCompanyId();
            var result = await _projectService.GetByIdAsync(id, scopeCompanyId);

            if (!result.Succeeded)
            {
                return Forbid();
            }

            var project = result.Value!;

            Input = new UpdateProjectDto
            {
                Id = project.Id,
                Name = project.Name,
                Description = project.Description,
                StartDate = project.StartDate,
                EndDate = project.EndDate,
                Status = project.Status
            };

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var scopeCompanyId = GetScopeCompanyId();
            var result = await _projectService.UpdateAsync(Input, scopeCompanyId);

            if (!result.Succeeded)
            {
                ModelState.AddModelError(string.Empty, result.Error!);
                return Page();
            }

            return RedirectToPage("/Projects/Details", new { id = Input.Id });
        }

        private int? GetScopeCompanyId()
        {
            return User.IsInRole(AppRoles.Admin)
                ? null
                : int.Parse(User.FindFirstValue("CompanyId")!);
        }
    }
}
