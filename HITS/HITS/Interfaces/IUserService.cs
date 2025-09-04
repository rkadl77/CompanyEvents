using HITS.Models.Entities;

namespace HITS.Interfaces
{
    public interface IUserService
    {
        Task<User> GetUserByIdAsync(string id);
        Task<IEnumerable<User>> GetPendingUsersAsync();
        Task<bool> ApproveUserAsync(string userId);
        Task<bool> DeleteUserAsync(string userId);
    }
}