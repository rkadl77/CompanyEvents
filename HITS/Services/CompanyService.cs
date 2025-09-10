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
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                _context.Companies.Add(company);
                await _context.SaveChangesAsync();

                var manager = await _context.Users.FindAsync(managerId);
                if (manager == null)
                    throw new Exception("Manager not found");

                if (!manager.IsApproved)
                    throw new Exception("Manager account is not approved yet");

                manager.CompanyId = company.Id;
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
                return company;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<Company> GetCompanyByIdAsync(Guid id)
        {
            return await _context.Companies
                .Include(c => c.Managers.Where(m => m.IsApproved)) 
                .Include(c => c.Events)
                .ThenInclude(e => e.Participants)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<IEnumerable<Company>> GetAllCompaniesAsync()
        {
            return await _context.Companies
                .Include(c => c.Managers.Where(m => m.IsApproved)) 
                .Include(c => c.Events.Where(e => e.Date > DateTime.Now)) 
                .OrderBy(c => c.Name)
                .ToListAsync();
        }

        public async Task<IEnumerable<Event>> GetCompanyEventsAsync(Guid companyId, bool upcomingOnly = true)
        {
            var query = _context.Events
                .Include(e => e.Company)
                .Include(e => e.Participants)
                .Where(e => e.CompanyId == companyId)
                .AsQueryable();

            if (upcomingOnly)
            {
                query = query.Where(e => e.Date > DateTime.Now);
            }

            return await query.OrderBy(e => e.Date).ToListAsync();
        }

        public async Task<Company> GetManagerCompanyAsync(string managerId)
        {
            var manager = await _context.Users
                .Include(u => u.Company)
                .ThenInclude(c => c.Managers.Where(m => m.IsApproved))
                .Include(u => u.Company)
                .ThenInclude(c => c.Events.Where(e => e.Date > DateTime.Now))
                .FirstOrDefaultAsync(u => u.Id == managerId && u.CompanyId != null);

            return manager?.Company;
        }

        public async Task<bool> AddManagerToCompanyAsync(Guid companyId, string managerId, string currentManagerId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var currentManager = await _context.Users.FindAsync(currentManagerId);
                if (currentManager == null || currentManager.CompanyId != companyId || !currentManager.IsApproved)
                    throw new UnauthorizedAccessException("You don't have permission to add managers to this company");

                var newManager = await _context.Users.FindAsync(managerId);
                if (newManager == null)
                    throw new Exception("Manager to add not found");

                if (!newManager.IsApproved)
                    throw new Exception("Manager to add is not approved yet");

                if (newManager.CompanyId != null)
                    throw new Exception("Manager is already associated with another company");

                newManager.CompanyId = companyId;
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> RemoveManagerFromCompanyAsync(Guid companyId, string managerId, string currentManagerId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var currentManager = await _context.Users.FindAsync(currentManagerId);
                if (currentManager == null || currentManager.CompanyId != companyId || !currentManager.IsApproved)
                    throw new UnauthorizedAccessException("You don't have permission to remove managers from this company");

                var managerToRemove = await _context.Users.FindAsync(managerId);
                if (managerToRemove == null || managerToRemove.CompanyId != companyId)
                    throw new Exception("Manager not found in this company");

                if (managerToRemove.Id == currentManagerId)
                    throw new Exception("You cannot remove yourself from the company");

                managerToRemove.CompanyId = null;
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}