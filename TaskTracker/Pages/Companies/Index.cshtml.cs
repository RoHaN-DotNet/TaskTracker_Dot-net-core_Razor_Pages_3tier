using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using System.Linq;
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

        // =========================================================
        // SEARCH / FILTER
        // =========================================================

        [BindProperty(SupportsGet = true)]
        public CompanySearchFilterDto Filter { get; set; } = new();

        // =========================================================
        // RESULT
        // =========================================================

        public PagedResult<CompanyDto>? Result { get; set; }

        // =========================================================
        // CREATE
        // =========================================================

        [BindProperty]
        public CreateCompanyDto CreateCompany { get; set; } = new();

        // =========================================================
        // UPDATE
        // =========================================================

        [BindProperty]
        public UpdateCompanyDto UpdateCompany { get; set; } = new();

        // =========================================================
        // GET
        // =========================================================

        public async Task OnGetAsync()
        {
            await LoadCompaniesAsync();
        }

        // =========================================================
        // CREATE COMPANY
        // =========================================================

        public async Task<IActionResult> OnPostCreateAsync()
        {
            // -----------------------------------------------------
            // Remove UpdateCompany validation errors.
            // This request is only for CreateCompany.
            // -----------------------------------------------------

            var updateKeys = ModelState.Keys
                .Where(k => k.StartsWith("UpdateCompany"))
                .ToList();

            foreach (var key in updateKeys)
            {
                ModelState.Remove(key);
            }

            // -----------------------------------------------------
            // Validate CreateCompany
            // -----------------------------------------------------

            if (!ModelState.IsValid)
            {
                await LoadCompaniesAsync();
                return Page();
            }

            // -----------------------------------------------------
            // Get logged-in user/admin ID
            // -----------------------------------------------------

            var actingUserId = GetActingUserId();

            // -----------------------------------------------------
            // Call service
            // -----------------------------------------------------

            var result = await _companyService.CreateAsync(
                CreateCompany,
                actingUserId
            );

            // -----------------------------------------------------
            // Service failed
            // -----------------------------------------------------

            if (!result.Succeeded)
            {
                ModelState.AddModelError(
                    string.Empty,
                    result.Error ?? "Unable to create company."
                );

                await LoadCompaniesAsync();
                return Page();
            }

            // -----------------------------------------------------
            // Success
            // -----------------------------------------------------

            return RedirectToPage();
        }

        // =========================================================
        // UPDATE COMPANY
        // =========================================================

        public async Task<IActionResult> OnPostEditAsync()
        {
            // -----------------------------------------------------
            // Remove CreateCompany validation errors.
            // This request is only for UpdateCompany.
            // -----------------------------------------------------

            var createKeys = ModelState.Keys
                .Where(k => k.StartsWith("CreateCompany"))
                .ToList();

            foreach (var key in createKeys)
            {
                ModelState.Remove(key);
            }

            // -----------------------------------------------------
            // Check model validation
            // -----------------------------------------------------

            if (!ModelState.IsValid)
            {
                await LoadCompaniesAsync();
                return Page();
            }

            // -----------------------------------------------------
            // Validate company ID
            // -----------------------------------------------------

            if (UpdateCompany.Id <= 0)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Invalid company ID."
                );

                await LoadCompaniesAsync();
                return Page();
            }

            // -----------------------------------------------------
            // Get logged-in user/admin ID
            // -----------------------------------------------------

            var actingUserId = GetActingUserId();

            // -----------------------------------------------------
            // Call service
            // -----------------------------------------------------

            var result = await _companyService.UpdateAsync(
                UpdateCompany,
                actingUserId
            );

            // -----------------------------------------------------
            // Service failed
            // -----------------------------------------------------

            if (!result.Succeeded)
            {
                ModelState.AddModelError(
                    string.Empty,
                    result.Error ?? "Unable to update company."
                );

                await LoadCompaniesAsync();
                return Page();
            }

            // -----------------------------------------------------
            // Success
            // -----------------------------------------------------

            return RedirectToPage();
        }

        // =========================================================
        // LOAD COMPANIES
        // =========================================================

        private async Task LoadCompaniesAsync()
        {
            var result =
                await _companyService.SearchAsync(Filter);

            if (result.Succeeded)
            {
                Result = result.Value;
            }
            else
            {
                Result = null;
            }
        }

        // =========================================================
        // GET LOGGED-IN USER ID
        // =========================================================

        private int GetActingUserId()
        {
            var userIdClaim =
                User.FindFirst("UserId")?.Value
                ?? User.FindFirst("sub")?.Value
                ?? User.FindFirst(
                    ClaimTypes.NameIdentifier
                )?.Value;

            if (int.TryParse(userIdClaim, out var userId))
            {
                return userId;
            }

            throw new InvalidOperationException(
                "Unable to determine the logged-in user ID."
            );
        }
    }
}