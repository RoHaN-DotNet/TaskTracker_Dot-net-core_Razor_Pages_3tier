using TaskTrackerBLL.Interfaces.Security;
using TaskTrackerDAL.Interfaces;
namespace TaskTrackerBLL.Interfaces
{
    public interface IUnitOfWork:IDisposable
    {
        ICompanyFeature Companies { get; }

        IUserFeature Users { get; }

        IRoleFeature Roles { get; }

        IProjectFeature Projects { get; }//

        ITaskFeature Tasks { get; }//

        INotificationFeature Notifications { get; }//

        IAuditLogFeature AuditLogs { get; }//
        ITaskFileFeature TaskFiles { get; }//
        ITaskMemberFeature TaskMembers { get; }
        IUserRoleFeature UserRoles { get; }
        ITaskTransferHistoryFeature TaskTransferHistories { get; }
        Task<int> SaveChangesAsync();
    }
}
