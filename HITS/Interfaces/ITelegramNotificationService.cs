using HITS.Models.Entities;
using System.Threading.Tasks;

namespace HITS.Interfaces
{
    public interface ITelegramNotificationService
    {
        Task NotifyUserApprovedAsync(User user);
        Task NotifyUserApprovedAsync(string userId, string message);
        Task NotifyEventRegistrationAsync(User user, Event eventObj);
        Task NotifyEventCreatedAsync(Event eventObj);
    }
}
