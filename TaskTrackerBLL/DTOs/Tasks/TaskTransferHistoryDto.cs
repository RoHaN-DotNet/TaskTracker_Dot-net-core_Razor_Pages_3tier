using System;
using System.Collections.Generic;
using System.Text;

namespace TaskTrackerBLL.DTOs.Tasks
{
    public class TaskTransferHistoryDto
    {
        public int Id { get; set; }
        public int TaskId { get; set; }

        public int FromUserId { get; set; }
        public string FromUserName { get; set; } = string.Empty;

        public int ToUserId { get; set; }
        public string ToUserName { get; set; } = string.Empty;

        public int TransferredByUserId { get; set; }
        public string TransferredByUserName { get; set; } = string.Empty;

        public string Note { get; set; } = string.Empty;

        public DateTime TransferredAt { get; set; }
    }
}
