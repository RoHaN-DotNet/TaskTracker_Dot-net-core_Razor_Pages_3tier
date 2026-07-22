using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TaskTrackerBLL.DTOs.Company;
using TaskTrackerBLL.Interfaces.Services;

namespace TaskTracker.Pages.Companies
{
    public class DetailsModel : PageModel
    {
        private readonly ICompanyService _companyService;

        public DetailsModel(ICompanyService companyService)
        {
            _companyService = companyService;
        }

        public CompanyDto Company { get; set; } = default!;

        public CompanyStatisticsDto? Statistics { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var companyResult = await _companyService.GetByIdAsync(id);

            if (!companyResult.Succeeded)
            {
                return NotFound();
            }

            Company = companyResult.Value!;

            var statisticsResult = await _companyService.GetStatisticsAsync(id);
            Statistics = statisticsResult.Succeeded ? statisticsResult.Value : null;

            return Page();
        }

        public async Task<IActionResult> OnPostDeactivateAsync(int id, int actingUserId)
        {
            await _companyService.DeactivateAsync(id,actingUserId);

            return RedirectToPage("/Companies/Details", new { id });
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id, int actingUserId)
        {
            var result = await _companyService.DeleteAsync(id,actingUserId);

            if (!result.Succeeded)
            {
                TempData["DeleteError"] = result.Error;
                return RedirectToPage("/Companies/Details", new { id });
            }

            return RedirectToPage("/Companies/Index");
        }
    }
}
