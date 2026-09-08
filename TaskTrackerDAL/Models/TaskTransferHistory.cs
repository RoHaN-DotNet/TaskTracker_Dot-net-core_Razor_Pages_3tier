using System;
using System.Collections.Generic;
using System.Text;
using TaskTrackerDAL.Models.Common;

namespace TaskTrackerDAL.Models
{
    public class TaskTransferHistory:BaseModel
    {
        public int TaskId { get; set; }
        public ProjectTask Task { get; set; } = null!;
        public int FromUserId { get; set; }
        public User FromUser { get; set; } = null!;
        public int ToUserId { get; set; }
        public User ToUser { get; set; } = null!;
        public int TransferredByUserId { get; set; }
        public User TransferredByUser { get; set; } = null!;
        public string Note { get; set; } = string.Empty;
    }
}
