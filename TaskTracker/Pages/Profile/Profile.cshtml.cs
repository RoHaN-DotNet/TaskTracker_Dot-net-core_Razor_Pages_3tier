using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using TaskTrackerBLL.DTOs.Auth;
using TaskTrackerBLL.DTOs.Profile;
using TaskTrackerBLL.DTOs.User;
using TaskTrackerBLL.Interfaces.Services;
using static TaskTrackerBLL.DTOs.Auth.ChangePasswordDto;

namespace TaskTracker.Pages.Profile
{
    public class ProfileModel : PageModel
    {
        private readonly IUserService _userService;

        public ProfileModel(IUserService userService)
        {
            _userService = userService;
        }


        // =========================================================
        // USER PROFILE
        // =========================================================

        public UserDto UserProfile { get; set; } = new();


        // =========================================================
        // EDIT PROFILE INPUT
        // =========================================================

        [BindProperty]
        public UpdateProfileDto UpdateProfile { get; set; } = new();


        // =========================================================
        // CHANGE PASSWORD INPUT
        // =========================================================

        [BindProperty]
        public ChangePasswordInput PasswordInput { get; set; } = new();


        // =========================================================
        // GET
        // =========================================================

        public async Task<IActionResult> OnGetAsync()
        {
            var userId = GetCurrentUserId();

            if (userId == null)
            {
                return RedirectToPage("/Account/Login");
            }


            var result =
                await _userService.GetByIdAsync(
                    userId.Value);


            if (!result.Succeeded ||
                result.Value == null)
            {
                return NotFound();
            }


            UserProfile = result.Value;

            return Page();
        }


        // =========================================================
        // UPDATE PROFILE
        // =========================================================

        public async Task<IActionResult> OnPostUpdateProfileAsync()
        {
            var userId = GetCurrentUserId();

            if (userId == null)
            {
                return RedirectToPage("/Account/Login");
            }


            // Always use logged-in user's ID.
            // Never trust ID coming from browser.
            UpdateProfile.Id = userId.Value;


            // -----------------------------------------------------
            // Validation
            // -----------------------------------------------------
            /*
            if (!ModelState.IsValid)
            {
                await LoadUserProfileAsync(
                    userId.Value);

                return Page();
            }*/


            // -----------------------------------------------------
            // Update profile
            // -----------------------------------------------------

            var result =
                await _userService.UpdateProfileAsync(
                    UpdateProfile);


            if (!result.Succeeded)
            {
                TempData["ErrorMessage"] =
                    result.Error;

                return RedirectToPage();
            }


            TempData["SuccessMessage"] =
                "Profile updated successfully.";


            return RedirectToPage();
        }


        // =========================================================
        // CHANGE PASSWORD
        // =========================================================

        public async Task<IActionResult> OnPostChangePasswordAsync()
        {
            var userId = GetCurrentUserId();

            if (userId == null)
            {
                return RedirectToPage("/Account/Login");
            }


            // -----------------------------------------------------
            // Validation
            // -----------------------------------------------------
            /*
            if (!ModelState.IsValid)
            {
                await LoadUserProfileAsync(
                    userId.Value);

                return Page();
            }
            */

            // -----------------------------------------------------
            // Change password
            // -----------------------------------------------------

            var result =
                await _userService.ChangePasswordAsync(
                    userId.Value,
                    PasswordInput.CurrentPassword,
                    PasswordInput.NewPassword);


            if (!result.Succeeded)
            {
                await LoadUserProfileAsync(
                    userId.Value);


                ModelState.AddModelError(
                    "PasswordInput.CurrentPassword",
                    result.Error ??
                    "Unable to change password.");


                return Page();
            }


            TempData["SuccessMessage"] =
                "Password changed successfully.";


            return RedirectToPage();
        }


        // =========================================================
        // LOAD USER PROFILE
        // =========================================================

        private async Task LoadUserProfileAsync(
            int userId)
        {
            var result =
                await _userService.GetByIdAsync(userId);


            if (result.Succeeded &&
                result.Value != null)
            {
                UserProfile =
                    result.Value;
            }
        }


        // =========================================================
        // GET CURRENT USER ID
        // =========================================================

        private int? GetCurrentUserId()
        {
            var claim =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier);


            if (int.TryParse(
                claim,
                out int userId))
            {
                return userId;
            }


            return null;
        }
    }
}