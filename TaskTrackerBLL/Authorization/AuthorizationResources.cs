using System;
using System.Collections.Generic;
using System.Text;

namespace TaskTrackerBLL.Authorization
{
    public interface ICompanyScopedResource
    {
        int CompanyId { get; }
    }

    public interface IAssignableResource
    {
        bool IsAccessibleTo(int userId);
    }

    public sealed record CompanyScopedResource(int CompanyId) : ICompanyScopedResource;

    public sealed record ProjectAccessResource(int CompanyId, IReadOnlyList<int> MemberUserIds)
        : ICompanyScopedResource, IAssignableResource
    {
        public bool IsAccessibleTo(int userId) => MemberUserIds.Contains(userId);
    }

    public sealed record TaskAccessResource(int CompanyId, int? AssignedToUserId)
        : ICompanyScopedResource, IAssignableResource
    {
        public bool IsAccessibleTo(int userId) => AssignedToUserId == userId;
    }
}
