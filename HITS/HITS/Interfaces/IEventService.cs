using HITS.Models.Entities;

namespace HITS.Interfaces
{
    public interface IEventService
    {
        Task<Event> CreateEventAsync(Event newEvent, string managerId);
        Task<Event> GetEventByIdAsync(Guid id);
        Task<IEnumerable<Event>> GetAllEventsAsync(bool upcomingOnly = true); 
        Task<IEnumerable<Event>> GetCompanyEventsAsync(Guid companyId, bool upcomingOnly = true); 
        Task<IEnumerable<Event>> GetUserEventsAsync(string userId); 
        Task<IEnumerable<User>> GetEventParticipantsAsync(Guid eventId, string managerId);
        Task<bool> RegisterForEventAsync(Guid eventId, string studentId);
        Task<bool> DeleteEventAsync(Guid eventId);
        Task<bool> CheckRegistrationDeadlineAsync(Guid eventId);
    }
}