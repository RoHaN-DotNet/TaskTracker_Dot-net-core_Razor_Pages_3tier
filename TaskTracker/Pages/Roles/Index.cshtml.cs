using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TaskTrackerBLL.DTOs.Role;
using TaskTrackerBLL.Interfaces.Services;

namespace TaskTracker.Pages.Roles
{
    public class IndexModel : PageModel
    {
        private readonly IRoleService _roleService;

        public IndexModel(IRoleService roleService)
        {
            _roleService = roleService;
        }

        public IReadOnlyList<RoleDto> Roles { get; set; } = Array.Empty<RoleDto>();
        [BindProperty]
        public CreateRoleDto CreateRole { get; set; } = new();

        [BindProperty]
        public UpdateRoleDto UpdateRole { get; set; } = new();

        public string? ErrorMessage { get; set; }
        public async Task OnGetAsync()
        {
            var result = await _roleService.GetAllAsync();

            Roles = result.Succeeded ? result.Value! : Array.Empty<RoleDto>();
        }
        // =========================
        // CREATE
        // =========================
        public async Task<IActionResult> OnPostCreateAsync()
        {
            if (!ModelState.IsValid)
            {
                await LoadRolesAsync();
                return Page();
            }

            var result = await _roleService.CreateAsync(CreateRole);

            if (!result.Succeeded)
            {
                TempData["Error"] = result.Error;
                return RedirectToPage();
            }

            TempData["Success"] = "Role created successfully.";

            return RedirectToPage();
        }
        // =========================
        // UPDATE
        // =========================
        public async Task<IActionResult> OnPostUpdateAsync()
        {
            if (!ModelState.IsValid)
            {
                await LoadRolesAsync();
                return Page();
            }

            var result = await _roleService.UpdateAsync(UpdateRole);

            if (!result.Succeeded)
            {
                TempData["Error"] = result.Error;
                return RedirectToPage();
            }

            TempData["Success"] = "Role updated successfully.";

            return RedirectToPage();
        }

        // =========================
        // DELETE
        // =========================
        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var result = await _roleService.DeleteAsync(id);

            if (!result.Succeeded)
            {
                TempData["Error"] = result.Error;
                return RedirectToPage();
            }

            TempData["Success"] = "Role deleted successfully.";

            return RedirectToPage();
        }

        // =========================
        // LOAD ROLES
        // =========================
        private async Task LoadRolesAsync()
        {
            var result = await _roleService.GetAllAsync();

            if (result.Succeeded && result.Value != null)
            {
                Roles = result.Value;
            }
        }
    }
}
