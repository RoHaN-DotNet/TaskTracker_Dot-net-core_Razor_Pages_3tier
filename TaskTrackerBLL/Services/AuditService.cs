using System;
using System.Collections.Generic;
using System.Text;
using TaskTrackerBLL.Common;
using TaskTrackerBLL.DTOs.Audit;
using TaskTrackerBLL.Interfaces;
using TaskTrackerBLL.Interfaces.Services;
using TaskTrackerDAL.Interfaces;
using TaskTrackerDAL.Models;
using TaskTrackerDAL.Models.Enums;
using TaskTrackerDAL.Repositories;

namespace TaskTrackerBLL.Services
{
    public class AuditService:IAuditService
    {
        private readonly IUnitOfWork _unitOfWork;
       
        
        public AuditService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
           
        }
        public async Task LogCreatedAsync(string modelName, int modelId, int performedByUserId)
        {
            await WriteAsync(new AuditLog
            {
                ModelName = modelName,
                ModelId= modelId,
                ActionType=TaskTrackerDAL.Models.Enums.AuditActionType.Created,
                PerformedByUserId= performedByUserId,
                FieldName = null,
                OldValue = null,
                NewValue = null
            });
        }
        public async Task LogUpdatedAsync(string modelName, int modelId, int performedByUserId,
        string fieldName, string? oldValue, string? newValue)
        {
            if (oldValue == newValue)
            {
                return;
            }
            await WriteAsync(new AuditLog
            {
                ModelName = modelName,
                ModelId = modelId,
                ActionType = AuditActionType.Updated,
                PerformedByUserId = performedByUserId,
                FieldName = fieldName,
                OldValue = oldValue,
                NewValue = newValue
            });
        }
        public async Task LogDeletedAsync(string modelName, int modelId, int performedByUserId, string? snapshotSummary)
        {
            await WriteAsync(new AuditLog
            {
                ModelName = modelName,
                ModelId = modelId,
                ActionType = AuditActionType.Deleted,
                PerformedByUserId = performedByUserId,
                FieldName = null,
                OldValue = snapshotSummary,
                NewValue = null
            });
        }

        public async Task LogAssignedAsync(
            string modelName, int modelId, int performedByUserId,
            string? oldValue, string? newValue)
        {
            await WriteAsync(new AuditLog
            {
                ModelName = modelName,
                ModelId = modelId,
                ActionType = AuditActionType.Assigned,
                PerformedByUserId = performedByUserId,
                FieldName = null,
                OldValue = oldValue,
                NewValue = newValue
            });
        }

        public async Task<Result<IReadOnlyList<AuditLogDto>>> GetHistoryForEntityAsync(string entityName, int entityId)
        {
            var logs = await _unitOfWork.AuditLogs.GetForEntityAsync(entityName, entityId);

            return Result<IReadOnlyList<AuditLogDto>>.Success(logs.Select(MapToDto).ToList());
        }

        public async Task<Result<IReadOnlyList<AuditLogDto>>> GetActivityByUserAsync(
            int performedByUserId, int pageNumber, int pageSize)
        {
            var safePageNumber = pageNumber < 1 ? 1 : pageNumber;
            var safePageSize = pageSize is < 1 or > 100 ? 20 : pageSize;

            var logs = await _unitOfWork.AuditLogs.GetByUserAsync(performedByUserId, safePageNumber, safePageSize);

            return Result<IReadOnlyList<AuditLogDto>>.Success(logs.Select(MapToDto).ToList());
        }

        private async Task WriteAsync(AuditLog entry)
        {
            await _unitOfWork.AuditLogs.LogAsync(entry);
            await _unitOfWork.SaveChangesAsync();
        }

        private static AuditLogDto MapToDto(AuditLog log)
        {
            return new AuditLogDto
            {
                Id = log.Id,
                ModelName = log.ModelName,
                ModelId = log.ModelId,
                ActionType = log.ActionType,
                PerformedByUserName = log.PerformedByUser.FullName,
                FieldName = log.FieldName,
                OldValue = log.OldValue,
                NewValue = log.NewValue,
                CreatedAt = log.CreatedAt
            };
        }


    }
}
