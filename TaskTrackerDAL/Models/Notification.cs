using System;
using System.Collections.Generic;
using System.Text;
using TaskTrackerDAL.Models.Common;
using TaskTrackerDAL.Models.Enums;

namespace TaskTrackerDAL.Models
{
    public class Notification:BaseModel
    {
        public int RecipientUserId {  get; set; }
        public NotificationType Type { get; set; }
        public string Message { get; set; } = string.Empty;
        public int? RelatedTaskId { get; set; }
        public int? RelatedProjectId { get; set; }
        public bool IsRead {  get; set; }   
        public DateTime? ReadAt { get; set; }

        //Navigation Property

        public User RecipientUser { get; set; } = null!;
        public ProjectTask? RelatedTask { get; set; }
        public Project? RelatedProject {  get; set; }
        
    }
}
