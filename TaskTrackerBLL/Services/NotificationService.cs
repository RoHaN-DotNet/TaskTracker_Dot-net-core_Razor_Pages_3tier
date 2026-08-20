using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using TaskTrackerBLL.Common;
using TaskTrackerBLL.DTOs.Notification;
using TaskTrackerBLL.Interfaces;
using TaskTrackerBLL.Interfaces.Services;

using TaskTrackerDAL.Models;
using TaskTrackerDAL.Models.Enums;

namespace TaskTrackerBLL.Services
{
    public class NotificationService : INotificationService
    {
        private readonly IUnitOfWork _unitOfWork;

        public NotificationService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        // ============================================================
        // GET NOTIFICATIONS FOR USER
        // ============================================================

        public async Task<Result<IReadOnlyList<NotificationDto>>> GetForUserAsync(
            int userId,
            bool unreadOnly = false)
        {
            var notifications =
                await _unitOfWork.Notifications
                    .GetForUserAsync(userId, unreadOnly);

            var dtos = notifications
                .Select(n => new NotificationDto
                {
                    Id = n.Id,
                    Type = n.Type,
                    Message = n.Message,

                    RelatedTaskId = n.RelatedTaskId,
                    RelatedProjectId = n.RelatedProjectId,

                    IsRead = n.IsRead,
                    CreatedAt = n.CreatedAt
                })
                .ToList();

            return Result<IReadOnlyList<NotificationDto>>
                .Success(dtos);
        }

        // ============================================================
        // GET UNREAD COUNT
        // ============================================================

        public async Task<Result<int>> GetUnreadCountAsync(
            int userId)
        {
            var count =
                await _unitOfWork.Notifications
                    .CountUnreadAsync(userId);

            return Result<int>.Success(count);
        }

        // ============================================================
        // MARK AS READ
        // ============================================================

        public async Task<Result> MarkAsReadAsync(
            int notificationId,
            int userId)
        {
            var notification =
                await _unitOfWork.Notifications
                    .GetByIdAsync(notificationId);

            if (notification is null ||
                notification.RecipientUserId != userId)
            {
                return Result.Failure(
                    "Notification not found.");
            }

            if (!notification.IsRead)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.UtcNow;

                _unitOfWork.Notifications.Update(notification);

                await _unitOfWork.SaveChangesAsync();
            }

            return Result.Success();
        }

        // ============================================================
        // TASK ASSIGNED NOTIFICATION
        // ============================================================

        public async Task NotifyTaskAssignedAsync(
            int taskId,
            int assignedToUserId,
            string taskTitle)
        {
            await CreateAsync(
                recipientUserId: assignedToUserId,

                type: NotificationType.TaskAssigned,

                message:
                    $"You were assigned to task '{taskTitle}'.",

                relatedTaskId: taskId,

                relatedProjectId: null);
        }

        // ============================================================
        // TASK COMPLETED NOTIFICATION
        // ============================================================

        public async Task NotifyTaskCompletedAsync(
            int taskId,
            string taskTitle,
            int notifyUserId)
        {
            await CreateAsync(
                recipientUserId: notifyUserId,

                type: NotificationType.TaskCompleted,

                message:
                    $"Task '{taskTitle}' was marked as completed.",

                relatedTaskId: taskId,

                relatedProjectId: null);
        }

        // ============================================================
        // PROJECT CREATED NOTIFICATION
        // ============================================================

        public async Task NotifyProjectCreatedAsync(
            int projectId,
            string projectName,
            IEnumerable<int> memberUserIds)
        {
            foreach (var userId in memberUserIds.Distinct())
            {
                await CreateAsync(
                    recipientUserId: userId,

                    type: NotificationType.ProjectCreated,

                    message:
                        $"You were added to a new project: '{projectName}'.",

                    relatedTaskId: null,

                    relatedProjectId: projectId);
            }
        }

        // ============================================================
        // DEADLINE NOTIFICATIONS
        // ============================================================

        public async Task<int> CheckAndCreateDeadlineNotificationsAsync()
        {
            var today = DateTime.UtcNow.Date;
            var tomorrow = today.AddDays(1);

            var createdCount = 0;

            // --------------------------------------------------------
            // Get all tasks
            // --------------------------------------------------------

            var allTasks =
                await _unitOfWork.Tasks.GetAllAsync();

            foreach (var task in allTasks)
            {
                // ----------------------------------------------------
                // Ignore completed / cancelled tasks
                // ----------------------------------------------------

                if (task.Status == ProjectTasksStatus.Completed ||
                    task.Status == ProjectTasksStatus.Cancelled)
                {
                    continue;
                }

                // ----------------------------------------------------
                // Ignore tasks without deadline
                // ----------------------------------------------------

                if (!task.DueDate.HasValue)
                {
                    continue;
                }

                var dueDate = task.DueDate.Value.Date;

                // ----------------------------------------------------
                // Get all TaskMembers
                // ----------------------------------------------------

                var taskMembers =
                    await _unitOfWork.TaskMembers
                        .GetByTaskIdAsync(task.Id);

                if (taskMembers.Count == 0)
                {
                    continue;
                }

                // ====================================================
                // DUE TOMORROW
                // ====================================================

                if (dueDate == tomorrow)
                {
                    foreach (var member in taskMembers)
                    {
                        var alreadyNotified =
                            await _unitOfWork.Notifications
                                .DeadlineNotificationExistsAsync(
                                    task.Id,
                                    NotificationType.DeadlineTomorrow);

                        if (alreadyNotified)
                        {
                            continue;
                        }

                        await CreateAsync(
                            recipientUserId: member.UserId,

                            type:
                                NotificationType.DeadlineTomorrow,

                            message:
                                $"Task '{task.Title}' is due tomorrow.",

                            relatedTaskId: task.Id,

                            relatedProjectId: null);

                        createdCount++;
                    }
                }

                // ====================================================
                // DUE TODAY
                // ====================================================

                else if (dueDate == today)
                {
                    foreach (var member in taskMembers)
                    {
                        var alreadyNotified =
                            await _unitOfWork.Notifications
                                .DeadlineNotificationExistsAsync(
                                    task.Id,
                                    NotificationType.DeadlineToday);

                        if (alreadyNotified)
                        {
                            continue;
                        }

                        await CreateAsync(
                            recipientUserId: member.UserId,

                            type:
                                NotificationType.DeadlineToday,

                            message:
                                $"Task '{task.Title}' is due today.",

                            relatedTaskId: task.Id,

                            relatedProjectId: null);

                        createdCount++;
                    }
                }
            }

            return createdCount;
        }

        // ============================================================
        // CREATE NOTIFICATION
        // ============================================================

        private async Task CreateAsync(
            int recipientUserId,
            NotificationType type,
            string message,
            int? relatedTaskId,
            int? relatedProjectId)
        {
            var notification = new Notification
            {
                RecipientUserId = recipientUserId,

                Type = type,

                Message = message,

                RelatedTaskId = relatedTaskId,

                RelatedProjectId = relatedProjectId,

                IsRead = false
            };

            await _unitOfWork.Notifications
                .AddAsync(notification);

            await _unitOfWork.SaveChangesAsync();
        }
    }
}