using System;
using System.Collections.Generic;
using System.Text;

namespace TaskTrackerDAL.Models.Enums
{
    public enum NotificationType
    {
        TaskAssigned = 0,
        DeadlineTomorrow = 1,
        DeadlineToday = 2,
        TaskCompleted = 3,
        ProjectCreated = 4
    }
}
