using System;
using System.Collections.Generic;
using System.Text;
using TaskTrackerBLL.Common;
using TaskTrackerBLL.DTOs.Audit;

namespace TaskTrackerBLL.Interfaces.Services
{
    public interface IAuditService
    {
        Task LogCreatedAsync(string modelName, int modelId, int performedByUserId);
        Task LogUpdatedAsync(string entityName, int entityId, int performedByUserId,
        string fieldName, string? oldValue, string? newValue);

        Task LogDeletedAsync(string modelName, int modelId, int performedByUserId, string? snapshotSummary);
        Task LogAssignedAsync(
        string modelName, int modelId, int performedByUserId,
        string? oldValue, string? newValue);

        Task<Result<IReadOnlyList<AuditLogDto>>> GetHistoryForEntityAsync(string modelName, int modelId);

        Task<Result<IReadOnlyList<AuditLogDto>>> GetActivityByUserAsync(
            int performedByUserId, int pageNumber, int pageSize);
    }
}
