using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using TaskTrackerDAL.Data;
using TaskTrackerDAL.Interfaces;
using TaskTrackerDAL.Models;
using TaskTrackerDAL.Repositories.Generic;

namespace TaskTrackerDAL.Repositories
{
    public class NotificationRepository : GenericRepository<Notification>, INotificationFeature
    {
        public NotificationRepository(TaskTrackerDbContext context) : base(context)
        {

        }
        public async Task<IReadOnlyList<Notification>> GetForUserAsync(int userId, bool unreadonly)
        {
            var query = _context.Set<Notification>()
                .AsNoTracking()
                .Where(n => n.RecipientUserId == userId);
            if (unreadonly)
            {
                query = query.Where(n => !n.IsRead);
            }
            return await query
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();
        }
        public async Task<int> CountUnreadAsync(int userId)
        {
            return await _context.Set<Notification>()
                .AsNoTracking()
                .CountAsync(n =>
                    n.RecipientUserId == userId &&
                    !n.IsRead);
        }
        public async Task<bool> DeadlineNotificationExistsAsync(int taskId, TaskTrackerDAL.Models.Enums.NotificationType type)
        {
            var Date = DateTime.UtcNow.Date;

            return await _context.Set<Notification>()
                .AsNoTracking()
                .AnyAsync(n => n.RelatedTaskId == taskId
                && n.Type== type
                && n.CreatedAt.Date== Date
                );
        }

    }
}
