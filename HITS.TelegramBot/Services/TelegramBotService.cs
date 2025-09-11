using HITS.Interfaces;
using HITS.Models.DTOs;
using HITS.Models.Entities;
using HITS.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace HITS.TelegramBot.Services
{
    public class TelegramBotService : IHostedService
    {
        private readonly TelegramBotClient _botClient;
        private readonly ILogger<TelegramBotService> _logger;
        private readonly ApiClientService _apiClient;
        private readonly SimpleMappingService _mappingService;
        private readonly UserStateService _userStateService;
        private CancellationTokenSource _cts;

        public TelegramBotService(IConfiguration configuration,
                                   ILogger<TelegramBotService> logger,
                                   ApiClientService apiClient,
                                   SimpleMappingService mappingService,
                                   UserStateService userStateService)
        {
            _logger = logger;
            _apiClient = apiClient;
            _mappingService = mappingService;
            _userStateService = userStateService;
            _botClient = new TelegramBotClient(configuration["TelegramBot:Token"]);
            _cts = new CancellationTokenSource();
        }
        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting Telegram Bot...");

            int offset = 0;
            _ = Task.Run(async () =>
            {
                while (!_cts.Token.IsCancellationRequested)
                {
                    try
                    {
                        var updates = await _botClient.GetUpdatesAsync(offset, timeout: 100, cancellationToken: _cts.Token);

                        foreach (var update in updates)
                        {
                            offset = update.Id + 1;
                            _ = HandleUpdateAsync(update);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error getting updates");
                        await Task.Delay(1000, _cts.Token);
                    }
                }
            }, _cts.Token);

            _logger.LogInformation("Telegram Bot started");
            return Task.CompletedTask;
        }

        private async Task HandleUpdateAsync(Update update)
        {
            try
            {
                if (update.Type != UpdateType.Message || update.Message?.Text == null)
                    return;

                var message = update.Message;
                var chatId = message.Chat.Id;
                var messageText = message.Text;

                _logger.LogInformation($"Received message from {chatId}: {messageText}");

                if (messageText.StartsWith("/"))
                {
                    await HandleCommand(chatId, messageText);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling update");
            }
        }

        private async Task HandleCommand(long chatId, string command)
        {
            try
            {
                var commandParts = command.Split(' ');
                var mainCommand = commandParts[0].ToLower();

                switch (mainCommand)
                {
                    case "/start":
                        await HandleStartCommand(chatId);
                        break;

                    case "/login":
                        await HandleLoginCommand(chatId, commandParts);
                        break;

                    case "/register":
                        await HandleRegisterCommand(chatId, commandParts);
                        break;

                    case "/events":
                        await HandleEventsCommand(chatId);
                        break;

                    case "/myevents":
                        await HandleMyEventsCommand(chatId);
                        break;

                    case "/registerevent":
                        await HandleRegisterEventCommand(chatId, commandParts);
                        break;

                    case "/logout":
                        await HandleLogoutCommand(chatId);
                        break;

                    default:
                        await HandleUnknownCommand(chatId);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling command");
                await _botClient.SendTextMessageAsync(chatId, "Произошла ошибка при обработке команды");
            }
        }

        private async Task HandleStartCommand(long chatId)
        {
            var userState = _userStateService.GetUserState(chatId);

            var message = "Добро пожаловать в HITS System! 🎓\n\n";

            if (userState.IsAuthenticated)
            {
                message += $"✅ Вы авторизованы как {userState.UserRole}\n";
                message += $"📊 Статус: {(userState.IsApproved ? "Подтвержден" : "Ожидает подтверждения")}\n\n";
            }

            message += "📋 *Доступные команды:*\n" +
                      "/register email password firstName lastName role - регистрация\n" +
                      "/login email password - вход\n" +
                      "/logout - выход\n";

            if (userState.IsAuthenticated && userState.IsApproved)
            {
                message += "/events - все мероприятия\n" +
                          "/myevents - мои мероприятия\n" +
                          "/registerevent eventId - запись на мероприятие\n";
            }

            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: message,
                parseMode: ParseMode.Markdown);
        }

        private async Task HandleLoginCommand(long chatId, string[] commandParts)
        {
            if (commandParts.Length >= 3)
            {
                var email = commandParts[1];
                var password = commandParts[2];

                try
                {
                    AuthResponse authResponse = await _apiClient.LoginAsync(email, password);

                    var userState = new UserState
                    {
                        UserId = authResponse.User.Id,
                        UserRole = authResponse.User.Role,
                        IsApproved = authResponse.User.IsApproved,
                        JwtToken = authResponse.Token
                    };

                    _userStateService.SetUserState(chatId, userState);
                    _mappingService.AddMapping(authResponse.User.Id, chatId);

                    await _botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: $"✅ Вход выполнен!\nID: {authResponse.User.Id}\nРоль: {authResponse.User.Role}\nСтатус: {(authResponse.User.IsApproved ? "Подтвержден" : "Ожидает подтверждения")}");
                }
                catch (Exception ex)
                {
                    await _botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: $"Ошибка входа: {ex.Message}");
                }
            }
            else
            {
                await _botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "Неверный формат. Используйте: /login email password");
            }
        }

        private async Task HandleLogoutCommand(long chatId)
        {
            _userStateService.ClearUserState(chatId);
            _apiClient.Logout();

            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "✅ Вы вышли из системы");
        }

        private async Task HandleRegisterCommand(long chatId, string[] commandParts)
        {
            if (commandParts.Length >= 6)
            {
                var email = commandParts[1];
                var password = commandParts[2];
                var firstName = commandParts[3];
                var lastName = commandParts[4];
                var role = commandParts[5];

                try
                {
                    var registrationResult = await _apiClient.RegisterUserAsync(email, password, firstName, lastName, role);

                    if (registrationResult.Contains("успешна"))
                    {
                        AuthResponse loginResult = await _apiClient.LoginAsync(email, password);

                        var userState = new UserState
                        {
                            UserId = loginResult.User.Id,
                            UserRole = loginResult.User.Role,
                            IsApproved = loginResult.User.IsApproved,
                            JwtToken = loginResult.Token
                        };

                        _userStateService.SetUserState(chatId, userState);
                        _mappingService.AddMapping(loginResult.User.Id, chatId);

                        await _botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: registrationResult + $"\nВаш ID: {loginResult.User.Id}");
                    }
                    else
                    {
                        await _botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: registrationResult);
                    }
                }
                catch (Exception ex)
                {
                    await _botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: $"Ошибка регистрации: {ex.Message}");
                }
            }
            else
            {
                await _botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "Неверный формат. Используйте: /register email password firstName lastName role");
            }
        }

        private async Task HandleEventsCommand(long chatId)
        {
            var userState = _userStateService.GetUserState(chatId);

            if (!userState.IsAuthenticated)
            {
                await _botClient.SendTextMessageAsync(chatId, "❌ Сначала выполните /login");
                return;
            }

            if (!userState.IsApproved)
            {
                await _botClient.SendTextMessageAsync(chatId, "❌ Ваш аккаунт еще не подтвержден администратором");
                return;
            }

            try
            {
                var events = await _apiClient.GetEventsAsync(true);

                if (events == null || events.Count == 0)
                {
                    await _botClient.SendTextMessageAsync(chatId, "На данный момент нет доступных мероприятий.");
                    return;
                }

                var message = "🎯 *Доступные мероприятия:*\n\n";
                foreach (var eventObj in events)
                {
                    message += $"*{eventObj.Title}*\n";
                    message += $"📅 {eventObj.Date:dd.MM.yyyy HH:mm}\n";
                    message += $"📍 {eventObj.Location}\n";
                    message += $"👥 Участников: {eventObj.Participants?.Count ?? 0}\n";

                    if (eventObj.RegistrationDeadline.HasValue)
                        message += $"⏰ Дедлайн записи: {eventObj.RegistrationDeadline:dd.MM.yyyy HH:mm}\n";

                    message += $"ID: `{eventObj.Id}`\n\n";
                }

                await _botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: message,
                    parseMode: ParseMode.Markdown);
            }
            catch (Exception ex)
            {
                await _botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: $"Ошибка при получении мероприятий: {ex.Message}");
            }
        }

        private async Task HandleMyEventsCommand(long chatId)
        {
            var userState = _userStateService.GetUserState(chatId);

            if (!userState.IsAuthenticated)
            {
                await _botClient.SendTextMessageAsync(chatId, "❌ Сначала выполните /login");
                return;
            }

            if (!userState.IsApproved)
            {
                await _botClient.SendTextMessageAsync(chatId, "❌ Ваш аккаунт еще не подтвержден администратором");
                return;
            }

            try
            {
                // ИСПРАВЛЕНО: используем API клиент вместо прямого доступа к сервису
                var events = await _apiClient.GetUserEventsAsync();

                if (events == null || events.Count == 0)
                {
                    await _botClient.SendTextMessageAsync(chatId, "Вы не записаны ни на одно мероприятие.");
                    return;
                }

                var message = "🎯 *Мои мероприятия:*\n\n";
                foreach (var eventObj in events)
                {
                    message += $"*{eventObj.Title}*\n";
                    message += $"📅 {eventObj.Date:dd.MM.yyyy HH:mm}\n";
                    message += $"📍 {eventObj.Location}\n";
                    message += $"🏢 {eventObj.CompanyName}\n";
                    message += $"👥 Участников: {eventObj.Participants?.Count ?? 0}\n\n";
                }

                await _botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: message,
                    parseMode: ParseMode.Markdown);
            }
            catch (Exception ex)
            {
                await _botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: $"Ошибка при получении моих мероприятий: {ex.Message}");
            }
        }
        private async Task HandleRegisterEventCommand(long chatId, string[] commandParts)
        {
            var userState = _userStateService.GetUserState(chatId);

            if (!userState.IsAuthenticated)
            {
                await _botClient.SendTextMessageAsync(chatId, "❌ Сначала выполните /login");
                return;
            }

            if (!userState.IsApproved)
            {
                await _botClient.SendTextMessageAsync(chatId, "❌ Ваш аккаунт еще не подтвержден администратором");
                return;
            }

            if (commandParts.Length < 2)
            {
                await _botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "Используйте: /registerevent eventId");
                return;
            }

            try
            {
                if (!Guid.TryParse(commandParts[1], out var eventId))
                {
                    await _botClient.SendTextMessageAsync(chatId, "Неверный формат ID мероприятия.");
                    return;
                }

                var success = await _apiClient.RegisterForEventAsync(eventId);

                if (success)
                {
                    await _botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "✅ Вы успешно записались на мероприятие!");
                }
                else
                {
                    await _botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "❌ Не удалось записаться на мероприятие. Проверьте ID и попробуйте снова.");
                }
            }
            catch (Exception ex)
            {
                await _botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: $"Ошибка при записи на мероприятие: {ex.Message}");
            }
        }

        private async Task HandleUnknownCommand(long chatId)
        {
            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "Неизвестная команда. Используйте /start для списка команд");
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Stopping Telegram Bot...");
            _cts.Cancel();
            return Task.CompletedTask;
        }
    }
}
