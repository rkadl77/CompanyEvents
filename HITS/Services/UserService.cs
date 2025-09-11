using HITS.Data;
using HITS.Interfaces;
using HITS.Models.DTOs;
using HITS.Models.Entities;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HITS.Services
{
    public class UserService : IUserService
    {
        private readonly ApplicationDbContext _context;

        public UserService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<User> GetUserByIdAsync(string id)
        {
            return await _context.Users
                .Include(u => u.Company)
                .FirstOrDefaultAsync(u => u.Id == id);
        }

        public async Task<IEnumerable<UserDto>> GetPendingUsersAsync()
        {
            return await _context.Users
                .Where(u => !u.IsApproved && u.Role != "Deanery")
                .Select(u => new UserDto
                {
                    Id = u.Id,
                    Email = u.Email,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    Role = u.Role,
                    IsApproved = u.IsApproved,
                    RejectionReason = u.RejectionReason 
                })
                .ToListAsync();
        }

        public async Task<bool> ApproveUserAsync(string userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return false;

            user.IsApproved = true;
            user.RejectionReason = null; 
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteUserAsync(string userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return false;

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RejectUserAsync(string userId, string reason)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return false;

            user.IsApproved = false;
            user.RejectionReason = reason;

            await _context.SaveChangesAsync();
            return true;
        }
    }
}