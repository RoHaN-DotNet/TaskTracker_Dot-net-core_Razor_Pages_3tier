using System;
using System.Collections.Generic;
using System.Text;
using TaskTrackerDAL.Interfaces.Generic;
using TaskTrackerDAL.Models;

namespace TaskTrackerDAL.Interfaces
{
    public interface ITaskMemberFeature:IGenericFeature<TaskMember>
    {

            

            Task RemoveAsync(TaskMember taskMember);

            Task<List<TaskMember>> GetByTaskIdAsync(int taskId);

            Task<bool> ExistsAsync(int taskId, int userId);

            Task<List<TaskMember>> GetByUserIdAsync(int userId);

    }

}
