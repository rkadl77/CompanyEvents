using HITS.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HITS.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class GoogleAuthController : ControllerBase
    {
        private readonly IGoogleCalendarService _googleCalendarService;
        private readonly IConfiguration _configuration;

        public GoogleAuthController(IGoogleCalendarService googleCalendarService, IConfiguration configuration)
        {
            _googleCalendarService = googleCalendarService;
            _configuration = configuration;
        }

        [HttpGet("test-auth")]
        [AllowAnonymous]
        public IActionResult TestAuth()
        {
            var clientId = _configuration["GoogleCalendar:ClientId"];

            return Ok(new
            {
                Message = "Google OAuth configured successfully!",
                ClientId = clientId,
                HasClientId = !string.IsNullOrEmpty(clientId),
                HasClientSecret = !string.IsNullOrEmpty(_configuration["GoogleCalendar:ClientSecret"]),
                Timestamp = DateTime.UtcNow
            });
        }

        [HttpGet("auth-url")]
        public async Task<IActionResult> GetAuthUrl()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var redirectUri = $"{Request.Scheme}://{Request.Host}/api/googleauth/callback";
            var authUrl = await _googleCalendarService.GetAuthUrlAsync(userId, redirectUri);

            return Ok(new { AuthUrl = authUrl });
        }

        [HttpGet("callback")]
        [AllowAnonymous]
        public async Task<IActionResult> Callback([FromQuery] string code, [FromQuery] string state)
        {
            if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(state))
                return BadRequest("Invalid callback parameters");

            var redirectUri = $"{Request.Scheme}://{Request.Host}/api/googleauth/callback";
            var success = await _googleCalendarService.SaveTokensAsync(state, code, redirectUri);

            if (success)
            {
                return Redirect($"https://t.me/EventsHITSbot?start=google_connected");
            }

            return BadRequest("Failed to authenticate with Google Calendar");
        }

        [HttpGet("status")]
        public async Task<IActionResult> GetStatus()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var hasAccess = await _googleCalendarService.HasCalendarAccessAsync(userId);
            return Ok(new { HasAccess = hasAccess });
        }

        [HttpGet("debug-save")]
        [AllowAnonymous]
        public async Task<IActionResult> DebugSaveTokens([FromQuery] string userId, [FromQuery] string code)
        {
            try
            {
                var redirectUri = $"{Request.Scheme}://{Request.Host}/api/googleauth/callback";
                var success = await _googleCalendarService.SaveTokensAsync(userId, code, redirectUri);

                return Ok(new
                {
                    Success = success,
                    UserId = userId,
                    CodeLength = code?.Length,
                    RedirectUri = redirectUri
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    Error = ex.Message,
                    StackTrace = ex.StackTrace
                });
            }
        }
    }
}