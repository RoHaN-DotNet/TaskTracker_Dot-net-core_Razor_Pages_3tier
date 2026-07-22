using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using TaskTrackerBLL.DTOs.Dashboard;
using TaskTrackerBLL.Interfaces.Services;
using TaskTrackerDAL.Constants;

namespace TaskTracker.Pages.Dashboard
{
    [AllowAnonymous]
    public class IndexModel : PageModel
    {
        private readonly IDashboardService _dashboardService;

        public IndexModel(IDashboardService dashboardService)
        {
            _dashboardService= dashboardService;
        }
        public AdminDashboardDto? AdminDashboard { get; set; }
        public ManagerDashboardDto? ManagerDashboard { get; set; }
        public EmployeeDashboardDto? EmployeeDashboard { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            if (User.IsInRole(AppRoles.Admin))
            {
                var result = await _dashboardService.GetAdminDashboardAsync();
                if (!result.Succeeded)
                {
                    return StatusCode(500);
                }
                AdminDashboard = result.Value;
            }
            else if (User.IsInRole(AppRoles.Manager))
            {
                var companyId = int.Parse(User.FindFirstValue("CompanyId")!);
                var result = await _dashboardService.GetManagerDashboardAsync(companyId);
                if (!result.Succeeded)
                {
                    return Forbid();
                }
                ManagerDashboard = result.Value;
            }
            else
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var result = await _dashboardService.GetEmployeeDashboardAsync(userId);
                if (!result.Succeeded)
                {
                    return StatusCode(500);
                }
                EmployeeDashboard = result.Value;
            }
            return Page();
        }   
    }
}
