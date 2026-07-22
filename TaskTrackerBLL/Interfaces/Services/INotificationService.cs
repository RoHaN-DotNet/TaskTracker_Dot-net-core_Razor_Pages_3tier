using System;
using System.Collections.Generic;
using System.Text;
using TaskTrackerBLL.Common;
using TaskTrackerBLL.DTOs.Notification;

namespace TaskTrackerBLL.Interfaces.Services
{
    public interface INotificationService
    {
        Task<Result<IReadOnlyList<NotificationDto>>> GetForUserAsync(int userId, bool unreadOnly = false);

        Task<Result<int>> GetUnreadCountAsync(int userId);

        Task<Result> MarkAsReadAsync(int notificationId, int userId);

        Task NotifyTaskAssignedAsync(int taskId, int assignedToUserId, string taskTitle);

        Task NotifyTaskCompletedAsync(int taskId, string taskTitle, int notifyUserId);

        Task NotifyProjectCreatedAsync(int projectId, string projectName, IEnumerable<int> memberUserIds);

        Task<int> CheckAndCreateDeadlineNotificationsAsync();
    }
}
