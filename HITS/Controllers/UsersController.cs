using HITS.Interfaces;
using HITS.Models.DTOs;
using HITS.Models.Entities;
using HITS.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
        [Authorize(Roles = "Deanery")]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetPendingUsers() 
        {
            try
            {
                var users = await _userService.GetPendingUsersAsync();
                return Ok(users);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
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

        [HttpPost("{id}/reject")]
        [Authorize(Roles = "Deanery")]
        public async Task<IActionResult> RejectUser(string id, [FromBody] RejectUserDto rejectDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var result = await _userService.RejectUserAsync(id, rejectDto.Reason);

                if (!result)
                {
                    return NotFound(new { message = "User not found" });
                }

                try
                {
                    await _telegramNotificationService.NotifyUserRejectedAsync(id,
                        $"❌ Ваша заявка отклонена. Причина: {rejectDto.Reason}");
                }
                catch
                {
                }

                return Ok(new { message = "User rejected successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}