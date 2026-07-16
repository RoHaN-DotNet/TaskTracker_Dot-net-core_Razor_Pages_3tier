using System;
using System.Collections.Generic;
using System.Text;
using TaskTrackerDAL.Interfaces.Generic;
using TaskTrackerDAL.Models;

namespace TaskTrackerDAL.Interfaces
{
    public interface ICompanyFeature:IGenericFeature<Company>
    {
        Task<Company?> GetByIdWithUsersAsync(int companyId);

        Task<Company?> GetByIdWithProjectsAsync(int companyId);

        Task<bool> IsNameUniqueAsync(string name, int? excludeCompanyId = null);

        Task<IReadOnlyList<Company>> GetActiveCompaniesAsync();

        Task<(IReadOnlyList<Company> Items, int TotalCount)> SearchAsync(
            string? searchTerm,
            bool? isActive,
            int pageNumber,
            int pageSize);

        Task<bool> HasAnyUsersOrProjectsAsync(int companyId);
    }
}
