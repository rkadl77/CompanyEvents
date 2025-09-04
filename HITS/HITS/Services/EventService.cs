using HITS.Data;
using HITS.Interfaces;
using HITS.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace HITS.Services
{
    public class EventService : IEventService
    {
        private readonly ApplicationDbContext _context;

        public EventService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Event> CreateEventAsync(Event newEvent, string managerId)
        {
            var manager = await _context.Users.FindAsync(managerId);
            if (manager == null || manager.CompanyId == null)
                throw new Exception("Manager not found or not associated with company");

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

        public async Task<IEnumerable<Event>> GetAllEventsAsync()
        {
            return await _context.Events
                .Include(e => e.Company)
                .Where(e => e.Date > DateTime.Now)
                .ToListAsync();
        }

        public async Task<IEnumerable<Event>> GetCompanyEventsAsync(Guid companyId)
        {
            return await _context.Events
                .Include(e => e.Company)
                .Include(e => e.Participants)
                .Where(e => e.CompanyId == companyId)
                .ToListAsync();
        }

        public async Task<bool> RegisterForEventAsync(Guid eventId, string studentId)
        {
            var eventObj = await _context.Events
                .Include(e => e.Participants)
                .FirstOrDefaultAsync(e => e.Id == eventId);

            var student = await _context.Users.FindAsync(studentId);

            if (eventObj == null || student == null) return false;
            if (eventObj.RegistrationDeadline.HasValue && eventObj.RegistrationDeadline < DateTime.Now)
                return false;

            eventObj.Participants.Add(student);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteEventAsync(Guid eventId)
        {
            var eventObj = await _context.Events.FindAsync(eventId);
            if (eventObj == null) return false;

            _context.Events.Remove(eventObj);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}