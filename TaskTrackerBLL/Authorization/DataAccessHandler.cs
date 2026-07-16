using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using TaskTrackerDAL.Constants;

namespace TaskTrackerBLL.Authorization
{
    public class DataAccessHandler : AuthorizationHandler<DataAccessRequirement, object>
    {
        protected override Task HandleRequirementAsync(
       AuthorizationHandlerContext context,
       DataAccessRequirement requirement,
       object? resource)
        {
            // Rule 1: Admin can access everything, regardless of resource shape.
            if (context.User.IsInRole(AppRoles.Admin))
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            // Rule 2: Manager can access any resource that belongs to their own company.
            if (resource is ICompanyScopedResource companyResource
                && context.User.IsInRole(AppRoles.Manager))
            {
                var companyIdClaim = context.User.FindFirst("CompanyId")?.Value;

                if (int.TryParse(companyIdClaim, out var userCompanyId)
                    && userCompanyId == companyResource.CompanyId)
                {
                    context.Succeed(requirement);
                    return Task.CompletedTask;
                }
            }

            // Rule 3: Employees (Developer/Tester/Debugger/Designer) can access
            // only resources specifically assigned or linked to them.
            if (resource is IAssignableResource assignableResource
                && AppRoles.EmployeeRoles.Any(context.User.IsInRole))
            {
                var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (int.TryParse(userIdClaim, out var userId)
                    && assignableResource.IsAccessibleTo(userId))
                {
                    context.Succeed(requirement);
                    return Task.CompletedTask;
                }
            }

            // No rule matched: the requirement is left un-succeeded, which the
            // authorization framework treats as a denial by default.
            return Task.CompletedTask;
        }
    }
}
