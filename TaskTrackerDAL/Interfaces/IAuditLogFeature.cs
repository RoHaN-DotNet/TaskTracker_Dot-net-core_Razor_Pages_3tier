using System;
using System.Collections.Generic;
using System.Text;
using TaskTrackerDAL.Interfaces.Generic;
using TaskTrackerDAL.Models;

namespace TaskTrackerDAL.Interfaces
{
    public interface IAuditLogFeature:IGenericFeature<AuditLog>
    {
        Task<IReadOnlyList<AuditLog>> GetForEntityAsync(string modelName, int modelId);
        Task<IReadOnlyList<AuditLog>> GetByUserAsync(int performedByUserId, int pageNumber, int pageSize);
        Task LogAsync(AuditLog entry);
    }
}
