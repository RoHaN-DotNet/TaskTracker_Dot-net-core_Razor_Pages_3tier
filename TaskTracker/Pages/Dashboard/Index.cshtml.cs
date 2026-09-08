using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using TaskTrackerBLL.DTOs.Dashboard;
using TaskTrackerBLL.Interfaces.Services;
using TaskTrackerDAL.Constants;
using TaskTrackerDAL.Models.Enums;

namespace TaskTracker.Pages.Dashboard
{
    public class IndexModel : PageModel
    {
        private readonly IDashboardService _dashboardService;
        private readonly ITaskService _taskService;

        public IndexModel(IDashboardService dashboardService, ITaskService taskService)
        {
            _dashboardService = dashboardService;
            _taskService = taskService;
        }

        // Dashboard DTOs
        public AdminDashboardDto? AdminDashboard { get; set; }

        public ManagerDashboardDto? ManagerDashboard { get; set; }

        public EmployeeDashboardDto? EmployeeDashboard { get; set; }

        //Emp Dashboard
        public int NotStartedTaskCount { get; set; }

        public int InProgressTaskCount { get; set; }

        public int CompletedTaskCount { get; set; }

        public int CancelledTaskCount { get; set; }
        // =====================================================
        // ADMIN
        // =====================================================

        public async Task<IActionResult> OnGetAsync()
        {
            // -------------------------------------------------
            // ADMIN DASHBOARD
            // -------------------------------------------------

            if (User.IsInRole(AppRoles.Admin))
            {
                var result = await _dashboardService.GetAdminDashboardAsync();

                if (!result.Succeeded)
                {
                    return StatusCode(500);
                }

                AdminDashboard = result.Value;

                return Page();
            }


            // -------------------------------------------------
            // MANAGER DASHBOARD
            // -------------------------------------------------

            else if (User.IsInRole(AppRoles.Manager))
            {
                var companyIdValue =
                    User.FindFirstValue("CompanyId");

                if (string.IsNullOrEmpty(companyIdValue))
                {
                    return Forbid();
                }

                if (!int.TryParse(companyIdValue, out int companyId))
                {
                    return Forbid();
                }

                var result =
                    await _dashboardService
                        .GetManagerDashboardAsync(companyId);

                if (!result.Succeeded)
                {
                    return Forbid();
                }

                ManagerDashboard = result.Value;

                return Page();
            }


            // -------------------------------------------------
            // EMPLOYEE DASHBOARD
            // -------------------------------------------------

            else
            {
                // =====================================================
                // GET CURRENT EMPLOYEE ID
                // =====================================================

                var userIdValue =
                    User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (string.IsNullOrEmpty(userIdValue))
                {
                    return Forbid();
                }

                if (!int.TryParse(userIdValue, out int userId))
                {
                    return Forbid();
                }


                // =====================================================
                // EMPLOYEE DASHBOARD INFORMATION
                // =====================================================

                var dashboardResult =
                    await _dashboardService
                        .GetEmployeeDashboardAsync(userId);

                if (!dashboardResult.Succeeded)
                {
                    return StatusCode(500);
                }

                EmployeeDashboard = dashboardResult.Value;


                // =====================================================
                // GET ONLY TASKS ASSIGNED TO THIS EMPLOYEE
                // =====================================================

                var taskResult =
                    await _taskService
                        .GetByAssignedUserIdAsync(userId);

                if (taskResult.Succeeded &&
                    taskResult.Value != null)
                {
                    var employeeTasks =
                        taskResult.Value.ToList();


                    // NOT STARTED
                    NotStartedTaskCount =
                        employeeTasks.Count(t =>
                            t.Status ==
                            ProjectTasksStatus.NotStarted);


                    // IN PROGRESS
                    InProgressTaskCount =
                        employeeTasks.Count(t =>
                            t.Status ==
                            ProjectTasksStatus.InProgress);


                    // COMPLETED
                    CompletedTaskCount =
                        employeeTasks.Count(t =>
                            t.Status ==
                            ProjectTasksStatus.Completed);


                    // CANCELLED
                    CancelledTaskCount =
                        employeeTasks.Count(t =>
                            t.Status ==
                            ProjectTasksStatus.Cancelled);
                }


                return Page();
            }
        }


        // =====================================================
        // AJAX HANDLER — called from Index.cshtml when the
        // "Company/User/Project/Task Growth" time-range dropdown
        // changes. Returns JSON; does not re-render the page.
        // =====================================================

        public async Task<IActionResult> OnGetTrendsAsync(string range)
        {
            if (!User.IsInRole(AppRoles.Admin))
            {
                return Forbid();
            }

            var result = await _dashboardService.GetAdminDashboardTrendsAsync(range);

            if (!result.Succeeded)
            {
                return StatusCode(500);
            }

            return new JsonResult(result.Value);
        }
    }
}