using System;
using System.Collections.Generic;
using System.Text;

namespace TaskTrackerDAL.Models.Enums
{
    public enum AuditActionType
    {
        Created=0,
        Updated=1,
        Deleted=2,
        Assigned=3
    }
}
