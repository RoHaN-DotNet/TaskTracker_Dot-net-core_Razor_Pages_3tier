using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace TaskTracker.Pages.Account
{
    public class LogoutModel : PageModel
    {
        public async Task<IActionResult> OnPostLogoutAsync()
        {
            await HttpContext.SignOutAsync(
           CookieAuthenticationDefaults.AuthenticationScheme);

            return RedirectToPage("/Account/Login");
        }
        public void OnGet()
        {
        }
    }
}
