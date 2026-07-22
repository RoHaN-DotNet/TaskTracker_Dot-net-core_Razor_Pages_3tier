using Microsoft.Extensions.DependencyInjection;
using TaskTrackerBLL.Interfaces.Security;
using TaskTrackerBLL.Infrastucture.Security;
using TaskTrackerBLL.Infrastucture;
using TaskTrackerBLL.Interfaces;
using TaskTrackerBLL.Interfaces.Services;
using TaskTrackerBLL.Services;

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

            return services;
        }
    }
}
