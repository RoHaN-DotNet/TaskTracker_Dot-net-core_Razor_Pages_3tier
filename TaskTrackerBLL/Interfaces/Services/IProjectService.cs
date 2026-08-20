using System;
using System.Collections.Generic;
using System.Text;
using TaskTrackerBLL.Common;
using TaskTrackerBLL.DTOs.Project;

namespace TaskTrackerBLL.Interfaces.Services
{
    public interface IProjectService
    {
        Task<Result<ProjectDto>> GetByIdAsync(int id, int? actingManagerCompanyId);
        Task<Result<IReadOnlyList<ProjectDto>>> GetAllAsync(int? companyId);
        Task<Result<IReadOnlyList<ProjectDto>>> GetByMemberUserIdAsync(int userId);

        Task<Result<PagedResult<ProjectDto>>> SearchAsync(ProjectSearchFilterDto filter,int? actingManagerCompanyId);

        Task<Result<ProjectDto>> CreateAsync(CreateProjectDto dto,int createdByUserId,int? actingManagerCompanyId);

        Task<Result> UpdateAsync(UpdateProjectDto dto,int actingUserId, int? actingManagerCompanyId);

        Task<Result> ArchiveAsync(int projectId,int actingUserId, int? actingManagerCompanyId);

        Task<Result> AddMemberAsync(AssignProjectMemberDto dto,int actingUserId, int? actingManagerCompanyId);

        Task<Result> RemoveMemberAsync(int projectId, int userId, int? actingManagerCompanyId);
        Task<Result<PagedResult<ProjectDto>>> FilterAsync(
        ProjectFilterDto filter, int? actingManagerCompanyId);
        Task<Result<ProjectDto>> GetByIdWithMembersAsync(int projectId);
    }
}
