using TaskTrackerBLL.Common;
using TaskTrackerBLL.DTOs.Project;
using TaskTrackerBLL.Interfaces;
using TaskTrackerBLL.Interfaces.Services;
using TaskTrackerDAL.Models;
using TaskTrackerDAL.Models.Enums;
using TaskTrackerDAL.Repositories;

namespace TaskTrackerBLL.Services
{
    public class ProjectService:IProjectService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly INotificationService _notificationService;
        private readonly IAuditService _auditService;

        public ProjectService(IUnitOfWork unitOfWork, INotificationService notificationService, IAuditService auditService)

        {
            _unitOfWork = unitOfWork;
            _notificationService = notificationService;
            _auditService = auditService;
        }

        public async Task<Result<ProjectDto>> GetByIdAsync(int id, int? actingManagerCompanyId)
        {
            var project = await _unitOfWork.Projects.GetByIdWithMembersAsync(id);

            if (project is null)
            {
                return Result<ProjectDto>.Failure($"Project with ID {id} was not found.");
            }

            if (actingManagerCompanyId.HasValue && project.CompanyId != actingManagerCompanyId.Value)
            {
                return Result<ProjectDto>.Failure("You are not authorized to view this project.");
            }

            var dto = await BuildDtoAsync(project);

            return Result<ProjectDto>.Success(dto);
        }
        public async Task<Result<IReadOnlyList<ProjectDto>>> GetAllAsync(int? companyId)
        {
            IReadOnlyList<Project> projects;

            if (companyId.HasValue)
                projects = await _unitOfWork.Projects.GetByCompanyIdAsync(companyId.Value);
            else
                projects = await _unitOfWork.Projects.GetAllAsync();

            var dtos = new List<ProjectDto>();

            foreach (var project in projects)
            {
                dtos.Add(await BuildDtoAsync(project));
            }

            return Result<IReadOnlyList<ProjectDto>>.Success(dtos);
        }

        public async Task<Result<IReadOnlyList<ProjectDto>>> GetByMemberUserIdAsync(int userId)
        {
            var projects = await _unitOfWork.Projects.GetByMemberUserIdAsync(userId);

            var dtos = new List<ProjectDto>();
            foreach (var project in projects)
            {
                dtos.Add(await BuildDtoAsync(project));
            }

            return Result<IReadOnlyList<ProjectDto>>.Success(dtos);
        }

        public async Task<Result<PagedResult<ProjectDto>>> SearchAsync(
            ProjectSearchFilterDto filter,
            int? actingManagerCompanyId)
        {
            var pageNumber = filter.PageNumber < 1 ? 1 : filter.PageNumber;
            var pageSize = filter.PageSize is < 1 or > 100 ? 10 : filter.PageSize;

            var (items, totalCount) = await _unitOfWork.Projects.SearchByCompanyAsync(
                actingManagerCompanyId, filter.SearchTerm, filter.Status, pageNumber, pageSize);

            var dtos = new List<ProjectDto>();
            foreach (var project in items)
            {
                dtos.Add(await BuildDtoAsync(project));
            }

            var pagedResult = new PagedResult<ProjectDto>(dtos, pageNumber, pageSize, totalCount);

            return Result<PagedResult<ProjectDto>>.Success(pagedResult);
        }

        public async Task<Result<ProjectDto>> CreateAsync(
            CreateProjectDto dto,
            int createdByUserId,
            int? actingManagerCompanyId)
        {
            if (actingManagerCompanyId.HasValue && dto.CompanyId != actingManagerCompanyId.Value)
            {
                return Result<ProjectDto>.Failure(
                    "Managers may only create projects for their own company.");
            }

            var company = await _unitOfWork.Companies.GetByIdAsync(dto.CompanyId);
            if (company is null)
            {
                return Result<ProjectDto>.Failure($"Company with ID {dto.CompanyId} was not found.");
            }

            if (dto.EndDate.HasValue && dto.EndDate.Value < dto.StartDate)
            {
                return Result<ProjectDto>.Failure("End date cannot be earlier than the start date.");
            }

            var project = new Project
            {
                CompanyId = dto.CompanyId,
                Name = dto.Name,
                Description = dto.Description,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                Status = ProjectStatus.NotStarted,
                Priority = TaskPriority.Medium,
                CreatedByUserId = createdByUserId
            };

            foreach (var memberUserId in dto.InitialMemberUserIds.Distinct())
            {
                var isMemberOfCompany = await _unitOfWork.Users.GetByIdAsync(memberUserId);
                if (isMemberOfCompany is not null && isMemberOfCompany.CompanyId == dto.CompanyId)
                {
                    project.ProjectMembers.Add(new ProjectMember { UserId = memberUserId });
                }
            }

            await _unitOfWork.Projects.AddAsync(project);
            await _unitOfWork.SaveChangesAsync();

            var resultDto = await BuildDtoAsync(project);

            return Result<ProjectDto>.Success(resultDto);
        }

        public async Task<Result> UpdateAsync(UpdateProjectDto dto, int actingUserId, int? actingManagerCompanyId)
        {
            var project = await _unitOfWork.Projects.GetByIdAsync(dto.Id);

            if (project is null)
            {
                return Result.Failure($"Project with ID {dto.Id} was not found.");
            }

            if (actingManagerCompanyId.HasValue && project.CompanyId != actingManagerCompanyId.Value)
            {
                return Result.Failure("You are not authorized to update this project.");
            }

            if (project.Status == ProjectStatus.Archived)
            {
                return Result.Failure("Archived projects cannot be edited. Restore the project first.");
            }

            if (dto.EndDate.HasValue && dto.EndDate.Value < dto.StartDate)
            {
                return Result.Failure("End date cannot be earlier than the start date.");
            }

            if (dto.Status == ProjectStatus.Archived)
            {
                return Result.Failure("Use the Archive action to archive a project.");
            }

            var oldName = project.Name;
            var oldStatus = project.Status;
            var oldEndDate = project.EndDate;

            project.Name = dto.Name;
            project.Description = dto.Description;
            project.StartDate = dto.StartDate;
            project.EndDate = dto.EndDate;
            project.Status = dto.Status;
            project.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Projects.Update(project);
            await _unitOfWork.SaveChangesAsync();

            await _auditService.LogUpdatedAsync("Project", project.Id, actingUserId, "Name", oldName, dto.Name);
            await _auditService.LogUpdatedAsync(
                "Project", project.Id, actingUserId, "Status", oldStatus.ToString(), dto.Status.ToString());
            await _auditService.LogUpdatedAsync(
                "Project", project.Id, actingUserId, "EndDate", oldEndDate?.ToString("d"), dto.EndDate?.ToString("d"));

            return Result.Success();
        }

        public async Task<Result> ArchiveAsync(int projectId, int actingUserId, int? actingManagerCompanyId)
        {
            var project = await _unitOfWork.Projects.GetByIdAsync(projectId);

            if (project is null)
            {
                return Result.Failure($"Project with ID {projectId} was not found.");
            }

            if (actingManagerCompanyId.HasValue && project.CompanyId != actingManagerCompanyId.Value)
            {
                return Result.Failure("You are not authorized to archive this project.");
            }

            if (project.Status == ProjectStatus.Archived)
            {
                return Result.Failure($"'{project.Name}' is already archived.");
            }

            var oldStatus = project.Status;

            project.Status = ProjectStatus.Archived;
            project.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Projects.Update(project);
            await _unitOfWork.SaveChangesAsync();

            await _auditService.LogUpdatedAsync(
                "Project", project.Id, actingUserId, "Status", oldStatus.ToString(), "Archived");

            return Result.Success();
        }

        public async Task<Result> AddMemberAsync(AssignProjectMemberDto dto, int actingUserId, int? actingManagerCompanyId)
        {
            var project = await _unitOfWork.Projects.GetByIdWithMembersAsync(dto.ProjectId);
            if (project is null)
            {
                return Result.Failure($"Project with ID {dto.ProjectId} was not found.");
            }

            if (actingManagerCompanyId.HasValue && project.CompanyId != actingManagerCompanyId.Value)
            {
                return Result.Failure("You are not authorized to manage this project's team.");
            }

            var user = await _unitOfWork.Users.GetByIdAsync(dto.UserId);
            if (user is null)
            {
                return Result.Failure($"Employee with ID {dto.UserId} was not found.");
            }

            if (user.CompanyId != project.CompanyId)
            {
                return Result.Failure($"{user.FullName} does not belong to the same company as this project.");
            }

            var alreadyMember = await _unitOfWork.Projects.IsUserProjectMemberAsync(dto.ProjectId, dto.UserId);
            if (alreadyMember)
            {
                return Result.Failure($"{user.FullName} is already a member of this project.");
            }

            project.ProjectMembers.Add(new ProjectMember { ProjectId = dto.ProjectId, UserId = dto.UserId });

            //_unitOfWork.Projects.Update(project);
            await _unitOfWork.SaveChangesAsync();

            await _auditService.LogAssignedAsync(
                "Project", project.Id, actingUserId, oldValue: null, newValue: user.FullName);

            await _notificationService.NotifyProjectCreatedAsync(project.Id, project.Name, new[] { dto.UserId });

            return Result.Success();
        }

        public async Task<Result> RemoveMemberAsync(int projectId, int userId, int? actingManagerCompanyId)
        {
            var project = await _unitOfWork.Projects.GetByIdWithMembersAsync(projectId);

            if (project is null)
            {
                return Result.Failure($"Project with ID {projectId} was not found.");
            }

            if (actingManagerCompanyId.HasValue && project.CompanyId != actingManagerCompanyId.Value)
            {
                return Result.Failure("You are not authorized to manage this project's team.");
            }

            var membership = project.ProjectMembers.FirstOrDefault(pm => pm.UserId == userId);

            if (membership is null)
            {
                return Result.Failure("This user is not a member of the project.");
            }

            project.ProjectMembers.Remove(membership);
            _unitOfWork.Projects.Update(project);
            await _unitOfWork.SaveChangesAsync();

            return Result.Success();
        }
        public async Task<Result<ProjectDto>> GetByIdWithMembersAsync(int projectId)
        {
            var project =await _unitOfWork.Projects.GetByIdWithMembersAsync(projectId);

            if (project == null)
            {
                return Result<ProjectDto>.Failure("Project not found.");
            }

            var dto = new ProjectDto
            {
                Id = project.Id,
                CompanyId = project.CompanyId,
                Name = project.Name,
                Description = project.Description,
                StartDate = project.StartDate,
                EndDate = project.EndDate,
                Status = project.Status,
                CreatedAt = project.CreatedAt,

                Members = project.ProjectMembers
    .Where(pm => pm.User != null)
    .Select(pm => new ProjectMemberDto
    {
        UserId = pm.UserId,
        FullName = pm.User.FullName
    })
    .ToList()
            };

            return Result<ProjectDto>.Success(dto);
        }
        private async Task<ProjectDto> BuildDtoAsync(Project project)
        {
            var createdByUser = await _unitOfWork.Users.GetByIdAsync(project.CreatedByUserId);
            var totalTasks = await _unitOfWork.Tasks.CountAsync(t => t.ProjectId == project.Id);
            var completedTasks = await _unitOfWork.Tasks.CountAsync(
                t => t.ProjectId == project.Id && t.Status == ProjectTasksStatus.Completed);

            return new ProjectDto
            {
                Id = project.Id,
                CompanyId = project.CompanyId,
                Name = project.Name,
                Description = project.Description,
                StartDate = project.StartDate,
                EndDate = project.EndDate,
                Status = project.Status,
                CreatedByUserName = createdByUser?.FullName ?? "Unknown",
                TotalTasks = totalTasks,
                CompletedTasks = completedTasks,
                TeamMemberCount = project.ProjectMembers.Count,
                CreatedAt = project.CreatedAt,
                Members=project.ProjectMembers.Select(pm=> new ProjectMemberDto
                {
                    UserId = pm.UserId,
                    FullName = pm.User.FullName

                }).ToList()
                
            };
        }
        
        public async Task<Result<PagedResult<ProjectDto>>> FilterAsync(
    ProjectFilterDto filter, int? actingManagerCompanyId)
        {
            var pageNumber = filter.PageNumber < 1 ? 1 : filter.PageNumber;
            var pageSize = filter.PageSize is < 1 or > 100 ? 10 : filter.PageSize;

            var (items, totalCount) = await _unitOfWork.Projects.FilterAsync(
                filter.CompanyId,
                filter.Status,
                filter.Priority,
                filter.DeadlineFrom,
                filter.DeadlineTo,
                actingManagerCompanyId,
                pageNumber,
                pageSize);

            var dtos = new List<ProjectDto>();
            foreach (var project in items)
            {
                dtos.Add(await BuildDtoAsync(project));
            }

            return Result<PagedResult<ProjectDto>>.Success(new PagedResult<ProjectDto>(dtos, pageNumber, pageSize, totalCount));
        }
    }
}
