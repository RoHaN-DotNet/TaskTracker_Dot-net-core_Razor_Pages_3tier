using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using TaskTrackerBLL.Authorization;
using TaskTrackerBLL.DTOs.Task;
using TaskTrackerBLL.Interfaces.Services;
using TaskTrackerDAL.Constants;

namespace TaskTracker.Pages.Tasks
{
    public class DetailsModel : PageModel
    {
        private readonly ITaskService _taskService;
        private readonly IProjectService _projectService;
        private readonly IAuthorizationService _authorizationService;

        public DetailsModel(
            ITaskService taskService,
            IProjectService projectService,
            IAuthorizationService authorizationService)
        {
            _taskService = taskService;
            _projectService = projectService;
            _authorizationService = authorizationService;
        }

        public TaskDto Task { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var taskResult = await _taskService.GetByIdAsync(id);

            if (!taskResult.Succeeded)
            {
                return NotFound();
            }
            //ai solved
            var task = taskResult.Value!;
            int? companyId = User.IsInRole(AppRoles.Admin)
                             ? null
                             : int.Parse(User.FindFirstValue("CompanyId")!);
            var projectResult = await _projectService.GetByIdAsync(task.ProjectId,companyId);

            if (!projectResult.Succeeded)
            {
                return NotFound();
            }

            var resource = new TaskAccessResource(
                CompanyId: projectResult.Value!.CompanyId,
                AssignedToUserId: task.AssignedToUserId);

            var authResult = await _authorizationService.AuthorizeAsync(
                User, resource, AppPolicies.DataAccess);

            if (!authResult.Succeeded)
            {
                return Forbid();
            }

            Task = task;

            return Page();
        }
    }
}
