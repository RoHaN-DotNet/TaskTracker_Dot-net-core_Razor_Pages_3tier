using System;
using System.Collections.Generic;
using System.Text;
using TaskTrackerDAL.Models.Common;
using TaskTrackerDAL.Models.Enums;

namespace TaskTrackerDAL.Models
{
    public class AuditLog:BaseModel
    {
        public string ModelName { get; set; } = string.Empty;
        public int ModelId {  get; set; }
        public AuditActionType ActionType { get; set; }
        public int PerformedByUserId {  get; set; }
        public string? FieldName {  get; set; }
        public string? OldValue {  get; set; }
        public string? NewValue {  get; set; }
        //Navugation property
        public User PerformedByUser { get; set; } = null!;
    }
}
