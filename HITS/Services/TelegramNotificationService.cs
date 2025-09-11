using HITS.Data;
using HITS.Interfaces;
using HITS.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;

namespace HITS.Services
{
    public class TelegramNotificationService : ITelegramNotificationService
    {
        private readonly ApplicationDbContext _context;
        private readonly TelegramBotClient _botClient;
        private readonly ILogger<TelegramNotificationService> _logger;

        private const string DefaultApprovedMessage =
            "🎉 Ваш аккаунт подтвержден администратором!\n\n" +
            "Теперь вы можете:\n" +
            "• Просматривать все мероприятия\n" +
            "• Записываться на мероприятия\n" +
            "• Получать уведомления о новых событиях";

        public TelegramNotificationService(ApplicationDbContext context, IConfiguration configuration, ILogger<TelegramNotificationService> logger)
        {
            _context = context;
            _logger = logger;
            _botClient = new TelegramBotClient(configuration["TelegramBot:Token"]);
        }

        public async Task NotifyUserApprovedAsync(User user)
        {
            try
            {
                if (user == null)
                {
                    _logger.LogWarning("NotifyUserApprovedAsync called with null user");
                    return;
                }

                if (!string.IsNullOrEmpty(user.TelegramChatId))
                {
                    await _botClient.SendTextMessageAsync(
                        chatId: user.TelegramChatId,
                        text: DefaultApprovedMessage
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending approval notification to user {UserId}", user?.Id);
            }
        }

        public async Task NotifyUserApprovedAsync(string userId, string message)
        {
            try
            {
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("NotifyUserApprovedAsync called with empty userId");
                    return;
                }

                var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
                if (user == null)
                {
                    _logger.LogWarning("User with id {UserId} not found", userId);
                    return;
                }

                var textToSend = string.IsNullOrEmpty(message) ? DefaultApprovedMessage : message;
                if (!string.IsNullOrEmpty(user.TelegramChatId))
                {
                    await _botClient.SendTextMessageAsync(
                        chatId: user.TelegramChatId,
                        text: textToSend
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending approval notification to user {UserId}", userId);
            }
        }

        public async Task NotifyEventRegistrationAsync(User user, Event eventObj)
        {
            try
            {
                if (!string.IsNullOrEmpty(user.TelegramChatId))
                {
                    await _botClient.SendTextMessageAsync(
                        chatId: user.TelegramChatId,
                        text: $"✅ Вы успешно записались на мероприятие!\n\n" +
                             $"🎯 {eventObj.Title}\n" +
                             $"📅 {eventObj.Date:dd.MM.yyyy HH:mm}\n" +
                             $"📍 {eventObj.Location}\n\n" +
                             $"Не забудьте добавить в свой календарь!"
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending event registration notification to user {UserId}", user.Id);
            }
        }

        public async Task NotifyEventCreatedAsync(Event eventObj)
        {
            try
            {
                var users = await _context.Users
                    .Where(u => u.IsApproved && !string.IsNullOrEmpty(u.TelegramChatId))
                    .ToListAsync();

                foreach (var user in users)
                {
                    await _botClient.SendTextMessageAsync(
                        chatId: user.TelegramChatId,
                        text: $"🎉 Новое мероприятие!\n\n" +
                             $"🎯 {eventObj.Title}\n" +
                             $"📅 {eventObj.Date:dd.MM.yyyy HH:mm}\n" +
                             $"📍 {eventObj.Location}\n\n" +
                             $"Для записи используйте: /events"
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending event creation notification");
            }
        }
        public async Task NotifyUserRejectedAsync(string userId, string message)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null || user.TelegramChatId == null)
                    return;

                await _botClient.SendTextMessageAsync(
                    chatId: user.TelegramChatId,
                    text: message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending rejection notification to user {UserId}", userId);
            }
        }
    }
}
