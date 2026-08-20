using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TaskTrackerBLL.Infrastucture;
using TaskTrackerBLL.Infrastucture.Security;
using TaskTrackerBLL.Interfaces;
using TaskTrackerBLL.Interfaces.Security;
using TaskTrackerBLL.Interfaces.Services;
using TaskTrackerBLL.Services;
using TaskTrackerDAL.Models;

namespace TaskTrackerBLL
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            services.AddScoped<ICompanyService, CompanyService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IRoleService, RoleService>();
            services.AddScoped<IProjectService, ProjectService>();
            services.AddScoped<ITaskService, TaskService>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IDashboardService, DashboardService>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<IPasswordHasher, PasswordHasher>();
            services.AddScoped<IEmployeeService, EmployeeService>();
            services.AddScoped<IRoleAssignmentService, RoleAssignmentService>();
            services.AddScoped<IReportingService, ReportingService>();
            services.AddScoped<INotificationService, NotificationService>();
            services.AddScoped<IAuditService, AuditService>();
            services.AddScoped<ITaskFileService, TaskFileService>();
            

            return services;
        }
    }
}
