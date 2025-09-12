using HITS.Interfaces;
using HITS.Models.DTOs;
using HITS.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using HITS.Services;

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
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<EventDto>>> GetEvents(
            [FromQuery] bool upcomingOnly = true,
            [FromQuery] Guid? companyId = null)
        {
            try
            {
                IEnumerable<EventDto> events;

                if (companyId.HasValue)
                {
                    events = await _eventService.GetCompanyEventsAsync(companyId.Value, upcomingOnly);
                }
                else
                {
                    events = await _eventService.GetAllEventsAsync(upcomingOnly);
                }

                return Ok(events);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("my-events")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<EventDto>>> GetUserEvents()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            try
            {
                var events = await _eventService.GetUserEventsAsync(userId);
                return Ok(events);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<ActionResult<EventDto>> GetEvent(Guid id)
        {
            var eventObj = await _eventService.GetEventByIdAsync(id);
            if (eventObj == null)
            {
                return NotFound(new { message = "Event not found" });
            }
            return Ok(eventObj);
        }

        [HttpGet("{id}/participants")]
        [Authorize(Roles = "CompanyManager,Deanery")]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetEventParticipants(Guid id)
        {
            var managerId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(managerId))
            {
                return Unauthorized();
            }

            try
            {
                var participants = await _eventService.GetEventParticipantsAsync(id, managerId);
                return Ok(participants);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (Exception ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpGet("{id}/deadline")]
        [AllowAnonymous]
        public async Task<ActionResult> CheckRegistrationDeadline(Guid id)
        {
            var isRegistrationOpen = await _eventService.CheckRegistrationDeadlineAsync(id);

            if (!isRegistrationOpen)
            {
                return Ok(new
                {
                    isOpen = false,
                    message = "Registration deadline has passed or event not found"
                });
            }

            return Ok(new { isOpen = true, message = "Registration is open" });
        }

        [HttpPost]
        [Authorize(Roles = "CompanyManager,Deanery")]
        public async Task<ActionResult<EventDto>> CreateEvent(CreateEventDto createEventDto)
        {
            if (createEventDto.Date < DateTime.Now)
            {
                return BadRequest(new { message = "Event date cannot be in the past" });
            }

            if (createEventDto.RegistrationDeadline.HasValue &&
                createEventDto.RegistrationDeadline < DateTime.Now)
            {
                return BadRequest(new { message = "Registration deadline cannot be in the past" });
            }

            if (createEventDto.RegistrationDeadline.HasValue &&
                createEventDto.RegistrationDeadline >= createEventDto.Date)
            {
                return BadRequest(new { message = "Registration deadline must be earlier than the event date." });
            }

            var managerId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

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

                var eventDto = new EventDto
                {
                    Id = createdEvent.Id,
                    Title = createdEvent.Title,
                    Description = createdEvent.Description,
                    Date = createdEvent.Date,
                    Location = createdEvent.Location,
                    RegistrationDeadline = createdEvent.RegistrationDeadline,
                    CompanyId = createdEvent.Company.Id,
                    CompanyName = createdEvent.Company.Name
                };

                return CreatedAtAction(nameof(GetEvent), new { id = eventDto.Id }, eventDto);
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
            var studentId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(studentId))
            {
                return Unauthorized();
            }

            var isRegistrationOpen = await _eventService.CheckRegistrationDeadlineAsync(id);
            if (!isRegistrationOpen)
            {
                return BadRequest(new { message = "Registration deadline has passed" });
            }

            var result = await _eventService.RegisterForEventAsync(id, studentId);

            if (!result)
            {
                return BadRequest(new
                {
                    message = "Failed to register for event. Please check if you are approved and not already registered."
                });
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
                return NotFound(new { message = "Event not found" });
            }

            return NoContent();
        }
        [HttpPut("{id}")]
        [Authorize(Roles = "CompanyManager,Deanery")]
        public async Task<IActionResult> UpdateEvent(Guid id, [FromBody] UpdateEventDto updateEventDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (updateEventDto.RegistrationDeadline.HasValue && updateEventDto.RegistrationDeadline < DateTime.Now)
            {
                return BadRequest(new { message = "Registration deadline cannot be in the past" });
            }
            if (updateEventDto.Date.HasValue && updateEventDto.Date < DateTime.Now)
            {
                return BadRequest(new { message = "Event date cannot be in the past" });
            }

            var managerId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(managerId))
            {
                return Unauthorized();
            }

            try
            {
                var result = await _eventService.UpdateEventAsync(id, updateEventDto, managerId);

                if (!result)
                {
                    return NotFound(new { message = "Event not found" });
                }

                return Ok(new { message = "Event updated successfully" });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{id}/register")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> UnregisterFromEvent(Guid id)
        {
            var studentId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(studentId))
            {
                return Unauthorized();
            }

            var result = await _eventService.UnregisterFromEventAsync(id, studentId);

            if (!result)
            {
                return BadRequest(new { message = "Failed to unregister from event. You might not be registered for it." });
            }

            return Ok(new { message = "Successfully unregistered from event" });
        }
    }
}