using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using TaskTrackerBLL.DTOs.Company;
using TaskTrackerBLL.Interfaces.Services;

namespace TaskTracker.Pages.Companies
{
    public class EditModel : PageModel
    {
        private readonly ICompanyService _companyService;

        public EditModel(ICompanyService companyService)
        {
            _companyService = companyService;
        }

        [BindProperty]
        public UpdateCompanyDto Input { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var result = await _companyService.GetByIdAsync(id);

            if (!result.Succeeded)
            {
                return NotFound();
            }

            var company = result.Value!;

            Input = new UpdateCompanyDto
            {
                Id = company.Id,
                Name = company.Name,
                Email = company.Email,
                Phone = company.Phone,
                Address = company.Address,
                IsActive = company.IsActive
            };

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var actingUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            if (!ModelState.IsValid)
            {
                return Page();
            }


            var result = await _companyService.UpdateAsync(Input,actingUserId);

            if (!result.Succeeded)
            {
                ModelState.AddModelError(string.Empty, result.Error!);
                return Page();
            }

            return RedirectToPage("/Companies/Details", new { id = Input.Id });
        }
    }
}
