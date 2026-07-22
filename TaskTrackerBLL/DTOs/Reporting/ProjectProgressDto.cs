using System;
using System.Collections.Generic;
using System.Text;
using TaskTrackerDAL.Models.Enums;

namespace TaskTrackerBLL.DTOs.Reporting
{
    public class ProjectProgressDto
    {
        public int ProjectId {  get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public ProjectStatus Status { get; set; }
        public int TotalTasks {  get; set; }
        public int CompletedTasks {  get; set; }
        public double PercentComplete=>
            TotalTasks == 0 ? 0 : Math.Round(CompletedTasks*100.0/TotalTasks,1);
        public bool IsBehindSchedule {  get; set; }

    }
}
