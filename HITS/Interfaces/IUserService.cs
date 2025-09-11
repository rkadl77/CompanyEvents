using HITS.Models.DTOs;
using HITS.Models.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HITS.Interfaces
{
    public interface IUserService
    {
        Task<User> GetUserByIdAsync(string id); 
        Task<IEnumerable<UserDto>> GetPendingUsersAsync(); 
        Task<bool> ApproveUserAsync(string userId);
        Task<bool> DeleteUserAsync(string userId);
        Task<bool> RejectUserAsync(string userId, string reason);
    }
}