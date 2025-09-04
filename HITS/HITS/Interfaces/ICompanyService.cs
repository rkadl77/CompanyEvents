using HITS.Models.Entities;

namespace HITS.Interfaces
{
    public interface ICompanyService
    {
        Task<Company> CreateCompanyAsync(Company company, string managerId);
        Task<Company> GetCompanyByIdAsync(Guid id);
        Task<IEnumerable<Company>> GetAllCompaniesAsync();
        Task<bool> AddManagerToCompanyAsync(Guid companyId, string managerId);
    }
}