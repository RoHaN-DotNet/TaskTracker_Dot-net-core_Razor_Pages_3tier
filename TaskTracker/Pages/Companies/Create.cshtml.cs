using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TaskTrackerBLL.DTOs.Company;
using TaskTrackerBLL.Interfaces.Services;

namespace TaskTracker.Pages.Companies
{
    public class CreateModel : PageModel
    {
        private readonly ICompanyService _companyService;

        public CreateModel(ICompanyService companyService)
        {
            _companyService = companyService;
        }

        [BindProperty]
        public CreateCompanyDto Input { get; set; } = new();

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var result = await _companyService.CreateAsync(Input);

            if (!result.Succeeded)
            {
                ModelState.AddModelError(string.Empty, result.Error!);
                return Page();
            }

            return RedirectToPage("/Companies/Details", new { id = result.Value!.Id });
        }
    }
}
