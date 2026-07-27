using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TaskTrackerBLL.DTOs.Auth;
using TaskTrackerBLL.Interfaces.Security;
using TaskTrackerBLL.Interfaces.Services;

namespace TaskTracker.Pages.Account
{
    public class SignupModel : PageModel
    {
        private readonly IAuthService _authService;
        private readonly IPasswordHasher _passwordHasher;

        public SignupModel(IAuthService authService, IPasswordHasher passwordHasher)
        {
            _passwordHasher = passwordHasher;
            _authService = authService;
        }
        [BindProperty]
        public SignupDto Input { get; set; } = new();
        public string? SuccessMessage { get; set; }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var result = await _authService.SignupAsync(Input);
            if (!result.Succeeded)
            {
                ModelState.AddModelError("", result.Error!);
                return Page();

            }

            return RedirectToPage("/Account/Login");
        }

    }
}
