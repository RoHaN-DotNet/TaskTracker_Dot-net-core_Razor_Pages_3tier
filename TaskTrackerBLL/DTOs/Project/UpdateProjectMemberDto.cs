using System;
using System.Collections.Generic;
using System.Text;

namespace TaskTrackerBLL.DTOs.Project
{
    public class UpdateProjectMemberDto
    {
        public int ProjectId {  get; set; }
        public List<int> MemberUserIds { get; set; } = new();
    }
}
