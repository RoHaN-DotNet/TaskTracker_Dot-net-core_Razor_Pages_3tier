using System;
using System.Collections.Generic;
using System.Text;
using TaskTrackerDAL.Models.Enums;

namespace TaskTrackerBLL.DTOs.Audit
{
    public class AuditLogDto
    {
        public int Id { get; set; }

        public string ModelName { get; set; } = string.Empty;

        public int ModelId { get; set; }

        public AuditActionType ActionType { get; set; }

        public string PerformedByUserName { get; set; } = string.Empty;

        public string? FieldName { get; set; }

        public string? OldValue { get; set; }

        public string? NewValue { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
