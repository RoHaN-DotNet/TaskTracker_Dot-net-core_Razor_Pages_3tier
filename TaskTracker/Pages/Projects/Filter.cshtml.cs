using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Security.Claims;
using TaskTrackerBLL.Common;
using TaskTrackerBLL.DTOs.Project;
using TaskTrackerBLL.Interfaces.Services;
using TaskTrackerDAL.Constants;

namespace TaskTracker.Pages.Projects
{
    //[Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Manager}")]
    public class FilterModel : PageModel
    {
        private readonly IProjectService _projectService;
        private readonly ICompanyService _companyService;

        public FilterModel(IProjectService projectService, ICompanyService companyService)
        {
            _projectService = projectService;
            _companyService = companyService;
        }
        [BindProperty(SupportsGet = true)]
        public ProjectFilterDto Filter { get; set; } = new();

        public PagedResult<ProjectDto>? Result {  get; set; }
        public SelectList? CompanyOptions { get; set; }
        public  bool IsAdmin {  get; set; }
        public async Task OnGetAsync()
        {
            IsAdmin = User.IsInRole(AppRoles.Admin);

            int? scopeCompanyId = IsAdmin ? null : int.Parse(User.FindFirstValue("CompanyId")!);

            if (IsAdmin)
            {
                var companiesResult= await _companyService.GetAllAsync();
                var companies=companiesResult.Succeeded? companiesResult.Value! : new List<TaskTrackerBLL.DTOs.Company.CompanyDto>();
                CompanyOptions = new SelectList(companies, "Id", "Names");
            }
            var result=await _projectService.FilterAsync(Filter, scopeCompanyId);
            Result = result.Succeeded ? result.Value : null;
        }
    }
}
