using System;
using System.Collections.Generic;
using System.Text;
using TaskTrackerBLL.Common;
using TaskTrackerBLL.DTOs.Company;

namespace TaskTrackerBLL.Interfaces.Services
{
    public interface ICompanyService
    {
        Task<Result<CompanyDto>> GetByIdAsync(int id);

        Task<Result<IReadOnlyList<CompanyDto>>> GetAllAsync();

        Task<Result<PagedResult<CompanyDto>>> SearchAsync(CompanySearchFilterDto filter);

        Task<Result<CompanyDto>> CreateAsync(CreateCompanyDto dto);

        Task<Result> UpdateAsync(UpdateCompanyDto dto);

        Task<Result> DeactivateAsync(int id);

        Task<Result> DeleteAsync(int id);

        Task<Result<CompanyStatisticsDto>> GetStatisticsAsync(int id);

    }
}
