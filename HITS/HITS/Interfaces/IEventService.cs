using HITS.Models.Entities;

namespace HITS.Interfaces
{
    public interface IEventService
    {
        Task<Event> CreateEventAsync(Event newEvent, string managerId);
        Task<Event> GetEventByIdAsync(Guid id);
        Task<IEnumerable<Event>> GetAllEventsAsync();
        Task<IEnumerable<Event>> GetCompanyEventsAsync(Guid companyId);
        Task<bool> RegisterForEventAsync(Guid eventId, string studentId);
        Task<bool> DeleteEventAsync(Guid eventId);
    }
}