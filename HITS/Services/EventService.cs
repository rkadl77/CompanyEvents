using HITS.Data;
using HITS.Interfaces;
using HITS.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace HITS.Services
{
    public class EventService : IEventService
    {
        private readonly ApplicationDbContext _context;
        private readonly IGoogleCalendarService _googleCalendarService;

        public EventService(ApplicationDbContext context, IGoogleCalendarService googleCalendarService)
        {
            _context = context;
            _googleCalendarService = googleCalendarService;
        }

        public async Task<Event> CreateEventAsync(Event newEvent, string managerId)
        {
            var manager = await _context.Users
                .Include(u => u.Company)
                .FirstOrDefaultAsync(u => u.Id == managerId);

            if (manager == null)
                throw new Exception("Manager not found");

            if (manager.CompanyId == null)
                throw new Exception("Manager is not associated with any company");

            if (!manager.IsApproved)
                throw new Exception("Manager account is not approved yet");

            newEvent.CompanyId = manager.CompanyId.Value;
            _context.Events.Add(newEvent);
            await _context.SaveChangesAsync();
            return newEvent;
        }

        public async Task<Event> GetEventByIdAsync(Guid id)
        {
            return await _context.Events
                .Include(e => e.Company)
                .Include(e => e.Participants)
                .FirstOrDefaultAsync(e => e.Id == id);
        }

        public async Task<IEnumerable<Event>> GetAllEventsAsync(bool upcomingOnly = true)
        {
            var query = _context.Events
                .Include(e => e.Company)
                .Include(e => e.Participants)
                .AsQueryable();

            if (upcomingOnly)
            {
                query = query.Where(e => e.Date > DateTime.Now);
            }

            return await query.OrderBy(e => e.Date).ToListAsync();
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

        public async Task<IEnumerable<Event>> GetUserEventsAsync(string userId)
        {
            return await _context.Events
                .Include(e => e.Company)
                .Include(e => e.Participants)
                .Where(e => e.Participants.Any(p => p.Id == userId))
                .OrderBy(e => e.Date)
                .ToListAsync();
        }

        public async Task<IEnumerable<User>> GetEventParticipantsAsync(Guid eventId, string managerId)
        {
            var eventObj = await _context.Events
                .Include(e => e.Participants)
                .Include(e => e.Company)
                .ThenInclude(c => c.Managers)
                .FirstOrDefaultAsync(e => e.Id == eventId);

            if (eventObj == null)
                throw new Exception("Event not found");

            var isManagerAuthorized = eventObj.Company.Managers.Any(m => m.Id == managerId);
            if (!isManagerAuthorized)
                throw new UnauthorizedAccessException("Manager not authorized to view this event's participants");

            return eventObj.Participants.Where(p => p.IsApproved);
        }

        public async Task<bool> RegisterForEventAsync(Guid eventId, string studentId)
        {
            var eventObj = await _context.Events
                .Include(e => e.Participants)
                .FirstOrDefaultAsync(e => e.Id == eventId);

            var student = await _context.Users.FindAsync(studentId);

            if (eventObj == null) return false;
            if (student == null) return false;

            if (!student.IsApproved)
                throw new Exception("Account not approved. Please wait for administrator confirmation.");

            if (eventObj.RegistrationDeadline.HasValue && eventObj.RegistrationDeadline < DateTime.Now)
                throw new Exception("Registration deadline has passed.");

            if (eventObj.Participants.Any(p => p.Id == studentId))
                throw new Exception("You are already registered for this event.");

            if (eventObj.Date < DateTime.Now)
                throw new Exception("This event has already ended.");

            eventObj.Participants.Add(student);
            await _context.SaveChangesAsync();

            try
            {
                var hasCalendarAccess = await _googleCalendarService.HasCalendarAccessAsync(studentId);

                if (hasCalendarAccess)
                {
                    await _googleCalendarService.AddEventToCalendarAsync(studentId, eventObj);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to add event to Google Calendar: {ex.Message}");
            }

            return true;
        }

        public async Task<bool> CheckRegistrationDeadlineAsync(Guid eventId)
        {
            var eventObj = await _context.Events.FindAsync(eventId);
            if (eventObj == null)
                return false;

            return !eventObj.RegistrationDeadline.HasValue || eventObj.RegistrationDeadline >= DateTime.Now;
        }

        public async Task<bool> DeleteEventAsync(Guid eventId)
        {
            var eventObj = await _context.Events.FindAsync(eventId);
            if (eventObj == null)
                return false;

            _context.Events.Remove(eventObj);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}