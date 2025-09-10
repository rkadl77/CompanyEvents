using HITS.Interfaces;
using HITS.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HITS.Services;

namespace HITS.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Deanery")]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ITelegramNotificationService _telegramNotificationService;

        public UsersController(IUserService userService, ITelegramNotificationService telegramNotificationService)
        {
            _userService = userService;
            _telegramNotificationService = telegramNotificationService;
        }

        [HttpGet("pending")]
        public async Task<ActionResult<IEnumerable<User>>> GetPendingUsers()
        {
            var users = await _userService.GetPendingUsersAsync();
            return Ok(users);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<User>> GetUser(string id)
        {
            var user = await _userService.GetUserByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }
            return Ok(user);
        }

        [HttpPost("{id}/approve")]
        public async Task<IActionResult> ApproveUser(string id)
        {
            var user = await _userService.GetUserByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var result = await _userService.ApproveUserAsync(id);
            if (!result)
            {
                return NotFound();
            }

            await _telegramNotificationService.NotifyUserApprovedAsync(user.Id,
                "🎉 Ваш аккаунт подтвержден администратором!");

            return Ok(new { message = "User approved successfully" });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var result = await _userService.DeleteUserAsync(id);
            if (!result)
            {
                return NotFound();
            }

            return NoContent();
        }
    }
}