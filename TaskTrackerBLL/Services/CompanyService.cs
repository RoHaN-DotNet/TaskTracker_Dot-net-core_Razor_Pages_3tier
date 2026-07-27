using TaskTrackerBLL.Common;
using TaskTrackerBLL.DTOs.Company;
using TaskTrackerBLL.Interfaces;
using TaskTrackerBLL.Interfaces.Services;
using TaskTrackerDAL.Models;
using TaskTrackerDAL.Models.Enums;


namespace TaskTrackerBLL.Services
{
    public class CompanyService : ICompanyService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;

        public CompanyService(IUnitOfWork unitOfWork, IAuditService auditService)
        {
            _unitOfWork = unitOfWork;
            _auditService = auditService;
        }

        //Gets All the information of a company(Name, Users,UserCount,Projects,ProjectCount) 
        public async Task<Result<CompanyDto>> GetByIdAsync(int id)
        {
            var company = await _unitOfWork.Companies.GetByIdWithUsersAsync(id);

            if (company is null)
            {
                return Result<CompanyDto>.Failure($"Company with ID {id} was not found.");
            }

            var projectCount = await _unitOfWork.Projects.CountAsync(p => p.CompanyId == id);

            var dto = MapToDto(company, company.Users.Count, projectCount);

            return Result<CompanyDto>.Success(dto);
        }
        //Brings all the Companys from db as a list
        public async Task<Result<IReadOnlyList<CompanyDto>>> GetAllAsync()
        {
            var companies = await _unitOfWork.Companies.GetAllAsync();

            var dtos = new List<CompanyDto>();

            foreach (var company in companies)
            {
                var userCount = await _unitOfWork.Users.CountAsync(u => u.CompanyId == company.Id);
                var projectCount = await _unitOfWork.Projects.CountAsync(p => p.CompanyId == company.Id);

                dtos.Add(MapToDto(company, userCount, projectCount));
            }

            return Result<IReadOnlyList<CompanyDto>>.Success(dtos);
        }
        //Search, Filter
        public async Task<Result<PagedResult<CompanyDto>>> SearchAsync(CompanySearchFilterDto filter)
        {
            var pageNumber = filter.PageNumber < 1 ? 1 : filter.PageNumber;
            var pageSize = filter.PageSize is < 1 or > 100 ? 10 : filter.PageSize;

            var (items, totalCount) = await _unitOfWork.Companies.SearchAsync(
                filter.SearchTerm, filter.IsActive, pageNumber, pageSize);

            var dtos = new List<CompanyDto>();
            foreach (var company in items)
            {
                var userCount = await _unitOfWork.Users.CountAsync(u => u.CompanyId == company.Id);
                var projectCount = await _unitOfWork.Projects.CountAsync(p => p.CompanyId == company.Id);

                dtos.Add(MapToDto(company, userCount, projectCount));
            }

            var pagedResult = new PagedResult<CompanyDto>(dtos, pageNumber, pageSize, totalCount);

            return Result<PagedResult<CompanyDto>>.Success(pagedResult);
        }
        //Create Company and store the user Id of admin
        public async Task<Result<CompanyDto>> CreateAsync(CreateCompanyDto dto, int actingUserId)
        {
            var isUnique = await _unitOfWork.Companies.IsNameUniqueAsync(dto.Name);

            if (!isUnique)
            {
                return Result<CompanyDto>.Failure($"A company named '{dto.Name}' already exists.");
            }

            var company = new Company
            {
                Name = dto.Name,
                Email = dto.Email,
                Phone = dto.Phone,
                Address = dto.Address,
                IsActive = true
            };
            

            await _unitOfWork.Companies.AddAsync(company);
            await _unitOfWork.SaveChangesAsync();
            await _auditService.LogCreatedAsync("Company", company.Id, actingUserId);

            return Result<CompanyDto>.Success(MapToDto(company, 0, 0));
        }
        //Update Company and store the user id of admin
        public async Task<Result> UpdateAsync(UpdateCompanyDto dto, int actingUserId)
        {
            var company = await _unitOfWork.Companies.GetByIdAsync(dto.Id);

            if (company is null)
            {
                return Result.Failure($"Company with ID {dto.Id} was not found.");
            }

            var isUnique = await _unitOfWork.Companies.IsNameUniqueAsync(dto.Name, dto.Id);

            if (!isUnique)
            {
                return Result.Failure($"A company named '{dto.Name}' already exists.");
            }
            // Capture old values before overwriting, so each changed field can be audited individually.
            var oldName = company.Name;
            var oldEmail = company.Email;
            var oldPhone = company.Phone;
            var oldAddress = company.Address;
            var oldIsActive = company.IsActive;

            company.Name = dto.Name;
            company.Email = dto.Email;
            company.Phone = dto.Phone;
            company.Address = dto.Address;
            company.IsActive = dto.IsActive;
            company.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Companies.Update(company);
            await _unitOfWork.SaveChangesAsync();
            await _auditService.LogUpdatedAsync("Company", company.Id, actingUserId, "Name", oldName, dto.Name);
            await _auditService.LogUpdatedAsync("Company", company.Id, actingUserId, "Email", oldEmail, dto.Email);
            await _auditService.LogUpdatedAsync("Company", company.Id, actingUserId, "Phone", oldPhone, dto.Phone);
            await _auditService.LogUpdatedAsync("Company", company.Id, actingUserId, "Address", oldAddress, dto.Address);
            await _auditService.LogUpdatedAsync(
                "Company", company.Id, actingUserId, "IsActive", oldIsActive.ToString(), dto.IsActive.ToString());

            return Result.Success();
        }
        //Deactive the company and store the id of the admin
        public async Task<Result> DeactivateAsync(int id, int actingUserId)
        {
            var company = await _unitOfWork.Companies.GetByIdAsync(id);

            if (company is null)
            {
                return Result.Failure($"Company with ID {id} was not found.");
            }

            company.IsActive = false;
            company.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Companies.Update(company);
            await _unitOfWork.SaveChangesAsync();

            return Result.Success();
        }
        //Delete the company with no users and projects and store the admin id
        public async Task<Result> DeleteAsync(int id,int actingUserId)
        {
            var company = await _unitOfWork.Companies.GetByIdAsync(id);

            if (company is null)
            {
                return Result.Failure($"Company with ID {id} was not found.");
            }

            var hasDependents = await _unitOfWork.Companies.HasAnyUsersOrProjectsAsync(id);

            if (hasDependents)
            {
                return Result.Failure(
                    $"Cannot delete '{company.Name}' because it still has users or projects. " +
                    "Remove or reassign them first, or deactivate the company instead.");
            }
            var snapshot = $"{company.Name} ({company.Email})";

            _unitOfWork.Companies.Remove(company);
            await _unitOfWork.SaveChangesAsync();
            await _auditService.LogDeletedAsync("Company", id, actingUserId, snapshot);


            return Result.Success();
        }
        //Get company information like completed tasks, inprogress tasks etc
        public async Task<Result<CompanyStatisticsDto>> GetStatisticsAsync(int id)
        {
            var company = await _unitOfWork.Companies.GetByIdAsync(id);

            if (company is null)
            {
                return Result<CompanyStatisticsDto>.Failure($"Company with ID {id} was not found.");
            }

            var totalUsers = await _unitOfWork.Users.CountAsync(u => u.CompanyId == id);
            var activeUsers = await _unitOfWork.Users.CountAsync(u => u.CompanyId == id && u.IsActive);
            var totalProjects = await _unitOfWork.Projects.CountAsync(p => p.CompanyId == id);

            var projectsByStatus = new Dictionary<ProjectStatus, int>();
            foreach (ProjectStatus status in Enum.GetValues<ProjectStatus>())
            {
                projectsByStatus[status] = await _unitOfWork.Projects.CountByStatusAsync(id, status);
            }

            var totalTasks = 0;
            var completedTasks = 0;

            var companyProjects = await _unitOfWork.Projects.GetByCompanyIdAsync(id);
            foreach (var project in companyProjects)
            {
                totalTasks += await _unitOfWork.Tasks.CountAsync(t => t.ProjectId == project.Id);
                completedTasks += await _unitOfWork.Tasks.CountAsync(
                    t => t.ProjectId == project.Id && t.Status == ProjectTasksStatus.Completed);
            }

            var overdueTasks = await _unitOfWork.Tasks.GetOverdueTasksAsync(id);

            var dto = new CompanyStatisticsDto
            {
                CompanyId = company.Id,
                CompanyName = company.Name,
                TotalUsers = totalUsers,
                ActiveUsers = activeUsers,
                TotalProjects = totalProjects,
                ProjectsByStatus = projectsByStatus,
                TotalTasks = totalTasks,
                CompletedTasks = completedTasks,
                OverdueTasks = overdueTasks.Count
            };

            return Result<CompanyStatisticsDto>.Success(dto);
        }
        //converts company model into CompanyDto
        private static CompanyDto MapToDto(Company company, int totalUsers, int totalProjects)
        {
            return new CompanyDto
            {
                Id = company.Id,
                Name = company.Name,
                Email = company.Email,
                Phone = company.Phone,
                Address = company.Address,
                IsActive = company.IsActive,
                TotalUsers = totalUsers,
                TotalProjects = totalProjects,
                CreatedAt = company.CreatedAt
            };
        }
    }
}
