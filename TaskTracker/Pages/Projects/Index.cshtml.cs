using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using TaskTrackerBLL.Common;
using TaskTrackerBLL.DTOs.Project;
using TaskTrackerBLL.Interfaces.Services;
using TaskTrackerDAL.Constants;

namespace TaskTracker.Pages.Projects
{
    public class IndexModel : PageModel
    {
        private readonly IProjectService _projectService;

        public IndexModel(IProjectService projectService)
        {
            _projectService = projectService;
        }

        [BindProperty(SupportsGet = true)]
        public ProjectSearchFilterDto Filter { get; set; } = new();

        public PagedResult<ProjectDto>? PagedProjects { get; set; }

        public IReadOnlyList<ProjectDto> AssignedProjects { get; set; } = Array.Empty<ProjectDto>();

        public bool IsEmployeeView { get; private set; }

        public bool CanManage { get; private set; }

        public async Task OnGetAsync()
        {
            CanManage = User.IsInRole(AppRoles.Admin) || User.IsInRole(AppRoles.Manager);

            if (!CanManage)
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
        }
    }
}
