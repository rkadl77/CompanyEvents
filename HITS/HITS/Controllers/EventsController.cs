using HITS.Interfaces;
using HITS.Models.DTOs;
using HITS.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HITS.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EventsController : ControllerBase
    {
        private readonly IEventService _eventService;

        public EventsController(IEventService eventService)
        {
            _eventService = eventService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Event>>> GetEvents()
        {
            var events = await _eventService.GetAllEventsAsync();
            return Ok(events);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Event>> GetEvent(Guid id)
        {
            var eventObj = await _eventService.GetEventByIdAsync(id);
            if (eventObj == null)
            {
                return NotFound();
            }
            return Ok(eventObj);
        }

        [HttpPost]
        [Authorize(Roles = "CompanyManager,Deanery")]
        public async Task<ActionResult<Event>> CreateEvent(CreateEventDto createEventDto)
        {

            var managerId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(managerId))
            {
                return Unauthorized();
            }

            var newEvent = new Event
            {
                Title = createEventDto.Title,
                Description = createEventDto.Description,
                Date = createEventDto.Date,
                Location = createEventDto.Location,
                RegistrationDeadline = createEventDto.RegistrationDeadline
            };

            try
            {
                var createdEvent = await _eventService.CreateEventAsync(newEvent, managerId);
                return CreatedAtAction(nameof(GetEvent), new { id = createdEvent.Id }, createdEvent);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("{id}/register")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> RegisterForEvent(Guid id)
        {
            var studentId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(studentId))
            {
                return Unauthorized();
            }

            var result = await _eventService.RegisterForEventAsync(id, studentId);

            if (!result)
            {
                return BadRequest(new { message = "Failed to register for event" });
            }

            return Ok(new { message = "Successfully registered for event" });
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "CompanyManager,Deanery")]
        public async Task<IActionResult> DeleteEvent(Guid id)
        {
            var result = await _eventService.DeleteEventAsync(id);

            if (!result)
            {
                return NotFound();
            }

            return NoContent();
        }
    }
}