using HITS.Data;
using HITS.Interfaces;
using HITS.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace HITS.Services
{
    public class CompanyService : ICompanyService
    {
        private readonly ApplicationDbContext _context;

        public CompanyService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Company> CreateCompanyAsync(Company company, string managerId)
        {
            _context.Companies.Add(company);
            await _context.SaveChangesAsync();

            var manager = await _context.Users.FindAsync(managerId);
            if (manager != null)
            {
                manager.CompanyId = company.Id;
                await _context.SaveChangesAsync();
            }

            return company;
        }

        public async Task<Company> GetCompanyByIdAsync(Guid id)
        {
            return await _context.Companies
                .Include(c => c.Managers)
                .Include(c => c.Events)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<IEnumerable<Company>> GetAllCompaniesAsync()
        {
            return await _context.Companies
                .Include(c => c.Managers)
                .ToListAsync();
        }

        public async Task<bool> AddManagerToCompanyAsync(Guid companyId, string managerId)
        {
            var manager = await _context.Users.FindAsync(managerId);
            if (manager == null) return false;

            manager.CompanyId = companyId;
            await _context.SaveChangesAsync();
            return true;
        }
    }
}