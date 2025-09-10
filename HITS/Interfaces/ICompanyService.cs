using HITS.Models.Entities;

namespace HITS.Interfaces
{
    public interface ICompanyService
    {
        Task<Company> CreateCompanyAsync(Company company, string managerId);
        Task<Company> GetCompanyByIdAsync(Guid id);
        Task<IEnumerable<Company>> GetAllCompaniesAsync();
        Task<IEnumerable<Event>> GetCompanyEventsAsync(Guid companyId, bool upcomingOnly = true);
        Task<Company> GetManagerCompanyAsync(string managerId);
        Task<bool> AddManagerToCompanyAsync(Guid companyId, string managerId, string currentManagerId);
        Task<bool> RemoveManagerFromCompanyAsync(Guid companyId, string managerId, string currentManagerId);
    }
}