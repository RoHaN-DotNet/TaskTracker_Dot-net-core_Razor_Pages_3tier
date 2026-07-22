using System;
using System.Collections.Generic;
using System.Text;
using TaskTrackerDAL.Interfaces.Generic;
using TaskTrackerDAL.Models;

namespace TaskTrackerDAL.Interfaces
{
    public interface INotificationFeature:IGenericFeature<Notification>
    {
        Task<IReadOnlyList<Notification>> GetForUserAsync(int userId,bool unreadonly);
        Task<int> CountUnreadAsync(int userId);
        Task<bool> DeadlineNotificationExistsAsync(int taskId, TaskTrackerDAL.Models.Enums.NotificationType Type);

    }
}
