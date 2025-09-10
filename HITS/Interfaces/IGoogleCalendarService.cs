public interface IGoogleCalendarService
{
    Task<string> GetAuthUrlAsync(string userId, string redirectUri);
    Task<bool> SaveTokensAsync(string userId, string code, string redirectUri);
    Task<bool> AddEventToCalendarAsync(string userId, HITS.Models.Entities.Event eventObj);
    Task<bool> RemoveEventFromCalendarAsync(string userId, string calendarEventId);
    Task<bool> HasCalendarAccessAsync(string userId);
}