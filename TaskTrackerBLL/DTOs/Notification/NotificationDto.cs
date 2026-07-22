using System;
using System.Collections.Generic;
using System.Text;
using TaskTrackerDAL.Models.Enums;

namespace TaskTrackerBLL.DTOs.Notification
{
    public class NotificationDto
    {
        public int Id { get; set; }
        public NotificationType Type { get; set; }
        public string Message { get; set; } = string.Empty;

        public int? RelatedTaskId {  get; set; }
        public int? RelatedProjectId {  get; set; }
        public bool IsRead {  get; set; }
        public DateTime CreatedAt {  get; set; }

    }
}
