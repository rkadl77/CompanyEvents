using HITS.Models.DTOs;
using HITS.Models.Entities;

namespace HITS.Interfaces
{
    public interface IEventService
    {
        Task<Event> CreateEventAsync(Event newEvent, string managerId);
        Task<EventDto?> GetEventByIdAsync(Guid id);
        Task<IEnumerable<EventDto>> GetAllEventsAsync(bool upcomingOnly = true);
        Task<IEnumerable<EventDto>> GetCompanyEventsAsync(Guid companyId, bool upcomingOnly = true);
        Task<IEnumerable<EventDto>> GetUserEventsAsync(string userId);
        Task<IEnumerable<UserDto>> GetEventParticipantsAsync(Guid eventId, string managerId);
        Task<bool> RegisterForEventAsync(Guid eventId, string studentId);
        Task<bool> DeleteEventAsync(Guid eventId);
        Task<bool> CheckRegistrationDeadlineAsync(Guid eventId);
    }
}