using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TaskTrackerBLL.Common;
using TaskTrackerBLL.DTOs.Company;
using TaskTrackerBLL.Interfaces.Services;

namespace TaskTracker.Pages.Companies
{
    public class IndexModel : PageModel
    {
        private readonly ICompanyService _companyService;

        public IndexModel(ICompanyService companyService)
        {
            _companyService = companyService;
        }

        [BindProperty(SupportsGet = true)]
        public CompanySearchFilterDto Filter { get; set; } = new();

        public PagedResult<CompanyDto>? Result { get; set; }

        public async Task OnGetAsync()
        {
            var result = await _companyService.SearchAsync(Filter);

            Result = result.Succeeded ? result.Value : null;
        }
    }
}
