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

        public async Task OnGetAsync()
        {
            var result = await _roleService.GetAllAsync();

            Roles = result.Succeeded ? result.Value! : Array.Empty<RoleDto>();
        }
    }
}
