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

        public CompanyService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

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

        public async Task<Result<CompanyDto>> CreateAsync(CreateCompanyDto dto)
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

            return Result<CompanyDto>.Success(MapToDto(company, 0, 0));
        }

        public async Task<Result> UpdateAsync(UpdateCompanyDto dto)
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

            company.Name = dto.Name;
            company.Email = dto.Email;
            company.Phone = dto.Phone;
            company.Address = dto.Address;
            company.IsActive = dto.IsActive;
            company.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Companies.Update(company);
            await _unitOfWork.SaveChangesAsync();

            return Result.Success();
        }

        public async Task<Result> DeactivateAsync(int id)
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

        public async Task<Result> DeleteAsync(int id)
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

            _unitOfWork.Companies.Remove(company);
            await _unitOfWork.SaveChangesAsync();

            return Result.Success();
        }

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
