using HITS.Models.DTOs;
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
using Telegram.Bot.Types.ReplyMarkups;

namespace HITS.TelegramBot.Services
{
    public class TelegramBotService : IHostedService
    {
        private readonly TelegramBotClient _botClient;
        private readonly ILogger<TelegramBotService> _logger;
        private readonly ApiClientService _apiClient;
        private readonly UserStateService _userStateService;
        private CancellationTokenSource _cts;
        private readonly Dictionary<long, CreateEventState> _createEventStates = new();

        public TelegramBotService(IConfiguration configuration, ILogger<TelegramBotService> logger,
                                ApiClientService apiClient, UserStateService userStateService)
        {
            _logger = logger;
            _apiClient = apiClient;
            _userStateService = userStateService;
            _botClient = new TelegramBotClient(configuration["TelegramBot:Token"]);
            _cts = new CancellationTokenSource();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting Telegram Bot...");

            _ = Task.Run(async () =>
            {
                int lastUpdateId = 0;

                while (!_cts.Token.IsCancellationRequested)
                {
                    try
                    {
                        var updates = await _botClient.GetUpdatesAsync(
                            offset: lastUpdateId + 1,
                            timeout: 30,
                            cancellationToken: _cts.Token);

                        foreach (var update in updates)
                        {
                            lastUpdateId = update.Id;
                            _ = HandleUpdateAsync(update);
                        }

                        await Task.Delay(100, _cts.Token);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error in update loop");
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
                if (update.Type == UpdateType.Message && update.Message?.Text != null)
                {
                    await HandleMessage(update.Message);
                }
                else if (update.Type == UpdateType.CallbackQuery)
                {
                    await HandleCallbackQuery(update.CallbackQuery);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling update");
            }
        }

        private async Task HandleMessage(Message message)
        {
            var chatId = message.Chat.Id;
            var text = message.Text;

            if (_createEventStates.ContainsKey(chatId))
            {
                await HandleCreateEventStep(chatId, text);
                return;
            }

            if (text.StartsWith("/"))
            {
                await HandleCommand(chatId, text);
            }
        }
        private async Task HandleCommand(long chatId, string command)
        {
            var parts = command.Split(' ');
            var cmd = parts[0].ToLower();

            try
            {
                switch (cmd)
                {
                    case "/start": await HandleStartCommand(chatId); break;
                    case "/login": await HandleLoginCommand(chatId, parts); break;
                    case "/register": await HandleRegisterCommand(chatId, parts); break;
                    case "/logout": await HandleLogoutCommand(chatId); break;
                    case "/events": await HandleEventsCommand(chatId); break;
                    case "/myevents": await HandleMyEventsCommand(chatId); break;
                    case "/registerevent": await HandleRegisterEventCommand(chatId, parts); break;
                    case "/unregister": await HandleUnregisterCommand(chatId, parts); break;
                    case "/createevent": await StartCreateEvent(chatId); break;
                    case "/mycompany": await HandleMyCompanyCommand(chatId); break;
                    case "/participants": await HandleParticipantsCommand(chatId, parts); break;
                    case "/setdeadline": await HandleSetDeadlineCommand(chatId, parts); break;
                    case "/deleteevent": await HandleDeleteEventCommand(chatId, parts); break;
                    case "/editevent": await HandleEditEventCommand(chatId, parts); break;
                    case "/companies": await HandleCompaniesCommand(chatId); break;
                    case "/addmanager": await HandleAddManagerCommand(chatId, parts); break;
                    case "/pendingusers": await HandlePendingUsersCommand(chatId); break;
                    case "/approve": await HandleApproveCommand(chatId, parts); break;
                    case "/mycompanyevents":await HandleMyCompanyEventsCommand(chatId);break;
                    case "/reject":  await HandleRejectUserCommand(chatId, parts); break;
                    case "/help": await HandleHelpCommand(chatId); break;
                    default: await HandleUnknownCommand(chatId); break;
                }
            }
            catch (Exception ex)
            {
                await _botClient.SendTextMessageAsync(chatId, $"❌ Ошибка: {ex.Message}");
            }
        }
        private async Task HandleCallbackQuery(CallbackQuery callback)
        {
            var chatId = callback.Message.Chat.Id;
            var messageId = callback.Message.MessageId; 
            var data = callback.Data;

            try
            {
                if (data.StartsWith("approve_"))
                {
                    var userId = data.Split('_')[1];
                    await HandleApproveCallback(chatId, userId, messageId); 
                }
                else if (data.StartsWith("event_"))
                {
                    var parts = data.Split('_');
                    var eventId = Guid.Parse(parts[1]);
                    var action = parts[2];
                    await HandleEventAction(chatId, eventId, action);
                }
                await _botClient.AnswerCallbackQueryAsync(callback.Id);
            }
            catch (Exception ex)
            {
                await _botClient.SendTextMessageAsync(chatId, $"❌ Ошибка: {ex.Message}");
            }
        }
        private async Task HandleStartCommand(long chatId)
        {
            var state = _userStateService.GetUserState(chatId);
            var msg = "🎓 *HITS System - Управление мероприятиями*\n\n";

            if (state.IsAuthenticated)
            {
                msg += $"✅ *{state.UserRole}* • {(state.IsApproved ? "Подтвержден" : "Ожидает подтверждения")}\n\n";
            }

            msg += "📋 *Основные команды:*\n" +
                  "/login - Вход в систему\n" +
                  "/register - Регистрация\n" +
                  "/events - Все мероприятия\n" +
                  "/myevents - Мои мероприятия\n" +
                  "/help - Помощь по командам\n";

            if (state.IsAuthenticated && state.IsApproved)
            {
                if (state.UserRole == "Student")
                {
                    msg += "\n🎯 *Для студентов:*\n" +
                          "/registerevent - Запись на мероприятие\n" +
                          "/unregister - Отмена записи\n";
                }
                else if (state.UserRole == "CompanyManager")
                {
                    msg += "\n🏢 *Для менеджеров:*\n" +
                          "/createevent - Создать мероприятие\n" +
                          "/mycompany - Моя компания\n" +
                          "/mycompanyevents - Мероприятия моей компании\n" + 
                          "/participants - Участники мероприятия\n" +
                          "/setdeadline - Установить дедлайн\n" +
                          "/deleteevent - Удалить мероприятие\n" +
                          "/editevent - Редактировать мероприятие\n";
                }
                else if (state.UserRole == "Deanery")
                {
                    msg += "\n👨‍🎓 *Для деканата:*\n" +
                          "/pendingusers - Ожидающие подтверждения\n" +
                          "/approve - Подтвердить пользователя\n" +
                          "/reject - Отклонить пользователя\n" + 
                          "/companies - Все компании\n" +
                          "/addmanager - Добавить менеджера\n";
                }
            }

            await _botClient.SendTextMessageAsync(chatId, msg, ParseMode.Markdown);
        }
        private async Task HandleHelpCommand(long chatId)
        {
            var msg = "🎓 *HITS System - Помощь*\n\n" +
                     "📋 *Основные команды:*\n" +
                     "/start - Главное меню\n" +
                     "/login email password - Вход\n" +
                     "/register email password firstName lastName role - Регистрация\n" +
                     "/events - Все мероприятия\n" +
                     "/myevents - Мои мероприятия\n" +
                     "/logout - Выход\n\n" +
                     "🎯 *Для студентов:*\n" +
                     "/registerevent eventId - Запись на мероприятие\n" +
                     "/unregister eventId - Отмена записи\n\n" +
                     "🏢 *Для менеджеров:*\n" +
                     "/createevent - Создать мероприятие\n" +
                     "/mycompany - Моя компания\n" +
                     "/mycompanyevents - Мероприятия моей компании\n" + 
                     "/participants eventId - Участники мероприятия\n" +
                     "/setdeadline eventId dd.MM.yyyy HH:mm - Установить дедлайн\n" +
                     "/deleteevent eventId - Удалить мероприятие\n" +
                     "/editevent eventId - Редактировать мероприятие\n\n" +
                     "👨‍🎓 *Для деканата:*\n" +
                     "/pendingusers - Ожидающие подтверждения\n" +
                     "/approve userId - Подтвердить пользователя\n" +
                     "/reject userId причина - Отклонить пользователя\n" + 
                     "/companies - Все компании\n" +
                     "/addmanager companyId managerId - Добавить менеджера";

            await _botClient.SendTextMessageAsync(chatId, msg, ParseMode.Markdown);
        }
        private async Task HandleMyCompanyEventsCommand(long chatId)
        {
            if (!CheckAuth(chatId, "CompanyManager")) return;

            try
            {
                var events = await _apiClient.GetManagerEventsAsync();
                if (events.Count == 0)
                {
                    await _botClient.SendTextMessageAsync(chatId, "В вашей компании пока нет мероприятий");
                    return;
                }

                var message = "🎯 *Мероприятия вашей компании:*\n\n";
                foreach (var eventObj in events)
                {
                    message += $"*{eventObj.Title}*\n";
                    message += $"📅 {eventObj.Date:dd.MM.yyyy HH:mm}\n";
                    message += $"📍 {eventObj.Location}\n";
                    message += $"👥 Участников: {eventObj.Participants?.Count ?? 0}\n";
                    message += $"🆔 `{eventObj.Id}`\n\n";
                }

                await _botClient.SendTextMessageAsync(chatId, message, ParseMode.Markdown);
            }
            catch (Exception ex)
            {
                await _botClient.SendTextMessageAsync(chatId, $"❌ Ошибка: {ex.Message}");
            }
        }

        private async Task HandleRejectUserCommand(long chatId, string[] parts)
        {
            if (!CheckAuth(chatId, "Deanery")) return;

            if (parts.Length < 3)
            {
                await _botClient.SendTextMessageAsync(chatId, "❌ Формат: /reject userId причина");
                return;
            }

            try
            {
                var userId = parts[1];
                var reason = string.Join(" ", parts.Skip(2));

                var success = await _apiClient.RejectUserAsync(userId, reason);

                await _botClient.SendTextMessageAsync(chatId, success ?
                    "✅ Пользователь отклонен" : "❌ Не удалось отклонить пользователя");
            }
            catch (Exception ex)
            {
                await _botClient.SendTextMessageAsync(chatId, $"❌ Ошибка: {ex.Message}");
            }
        }
        private async Task HandleLoginCommand(long chatId, string[] parts)
        {
            if (parts.Length < 3)
            {
                await _botClient.SendTextMessageAsync(chatId, "❌ Формат: /login email password");
                return;
            }

            try
            {
                var response = await _apiClient.LoginAsync(parts[1], parts[2]);
                var state = new UserState
                {
                    UserId = response.User.Id,
                    UserRole = response.User.Role,
                    IsApproved = response.User.IsApproved,
                    JwtToken = response.Token
                };

                _userStateService.SetUserState(chatId, state);

                await _botClient.SendTextMessageAsync(chatId,
                    $"✅ Вход выполнен!\nID: {response.User.Id}\nРоль: {response.User.Role}\nСтатус: {(response.User.IsApproved ? "Подтвержден" : "Ожидает подтверждения")}");
            }
            catch (Exception ex)
            {
                await _botClient.SendTextMessageAsync(chatId, $"❌ Ошибка входа: {ex.Message}");
            }
        }
        private async Task HandleRegisterCommand(long chatId, string[] parts)
        {
            if (parts.Length < 6)
            {
                await _botClient.SendTextMessageAsync(chatId, "❌ Формат: /register email password firstName lastName role");
                return;
            }

            try
            {
                var result = await _apiClient.RegisterUserAsync(parts[1], parts[2], parts[3], parts[4], parts[5]);
                await _botClient.SendTextMessageAsync(chatId, result);
            }
            catch (Exception ex)
            {
                await _botClient.SendTextMessageAsync(chatId, $"❌ Ошибка регистрации: {ex.Message}");
            }
        }
        private async Task HandleLogoutCommand(long chatId)
        {
            _userStateService.ClearUserState(chatId);
            _apiClient.Logout();
            await _botClient.SendTextMessageAsync(chatId, "✅ Вы вышли из системы");
        }

        private async Task HandleRegisterEventCommand(long chatId, string[] parts)
        {
            if (!CheckAuth(chatId, "Student")) return;
            if (parts.Length < 2)
            {
                await _botClient.SendTextMessageAsync(chatId, "❌ Формат: /registerevent eventId");
                return;
            }

            try
            {
                if (!Guid.TryParse(parts[1], out var eventId))
                {
                    await _botClient.SendTextMessageAsync(chatId, "❌ Неверный формат ID мероприятия");
                    return;
                }

                var success = await _apiClient.RegisterForEventAsync(eventId);
                await _botClient.SendTextMessageAsync(chatId, success ?
                    "✅ Запись выполнена! Мероприятие добавлено в ваш Google Calendar 🗓️" :
                    "❌ Не удалось записаться на мероприятие");
            }
            catch (Exception ex)
            {
                await _botClient.SendTextMessageAsync(chatId, $"❌ Ошибка: {ex.Message}");
            }
        }
        private async Task HandleUnregisterCommand(long chatId, string[] parts)
        {
            if (!CheckAuth(chatId, "Student")) return;
            if (parts.Length < 2)
            {
                await _botClient.SendTextMessageAsync(chatId, "❌ Формат: /unregister eventId");
                return;
            }

            try
            {
                if (!Guid.TryParse(parts[1], out var eventId))
                {
                    await _botClient.SendTextMessageAsync(chatId, "❌ Неверный формат ID мероприятия");
                    return;
                }

                var success = await _apiClient.UnregisterFromEventAsync(eventId);
                await _botClient.SendTextMessageAsync(chatId, success ?
                    "✅ Запись отменена" : "❌ Не удалось отменить запись");
            }
            catch (Exception ex)
            {
                await _botClient.SendTextMessageAsync(chatId, $"❌ Ошибка: {ex.Message}");
            }
        }
        private async Task StartCreateEvent(long chatId)
        {
            if (!CheckAuth(chatId, "CompanyManager")) return;

            _createEventStates[chatId] = new CreateEventState { Step = 1 };
            await _botClient.SendTextMessageAsync(chatId,
                "🏗️ *Создание мероприятия*\n\nВведите название мероприятия:",
                ParseMode.Markdown);
        }

        private async Task HandleCreateEventStep(long chatId, string text)
        {
            var state = _createEventStates[chatId];

            try
            {
                switch (state.Step)
                {
                    case 1:
                        state.Title = text;
                        state.Step = 2;
                        await _botClient.SendTextMessageAsync(chatId, "📝 Введите описание (или /skip чтобы пропустить):");
                        break;

                    case 2:
                        if (text != "/skip") state.Description = text;
                        state.Step = 3;
                        await _botClient.SendTextMessageAsync(chatId, "📅 Введите дату и время (формат: dd.MM.yyyy HH:mm):");
                        break;

                    case 3:
                        if (DateTime.TryParse(text, out var date))
                        {
                            state.Date = date;
                            state.Step = 4;
                            await _botClient.SendTextMessageAsync(chatId, "📍 Введите место проведения:");
                        }
                        else
                        {
                            await _botClient.SendTextMessageAsync(chatId, "❌ Неверный формат даты");
                        }
                        break;

                    case 4:
                        state.Location = text;
                        state.Step = 5;
                        await _botClient.SendTextMessageAsync(chatId, "⏰ Введите дедлайн записи (формат: dd.MM.yyyy HH:mm) или /skip:");
                        break;

                    case 5:
                        if (text != "/skip" && DateTime.TryParse(text, out var deadline))
                            state.RegistrationDeadline = deadline;

                        var eventDto = new CreateEventDto
                        {
                            Title = state.Title,
                            Description = state.Description,
                            Date = state.Date,
                            Location = state.Location,
                            RegistrationDeadline = state.RegistrationDeadline
                        };

                        var createdEvent = await _apiClient.CreateEventAsync(eventDto);
                        _createEventStates.Remove(chatId);

                        await _botClient.SendTextMessageAsync(chatId,
                            $"✅ Мероприятие создано!\n\n*{createdEvent.Title}*\n" +
                            $"📅 {createdEvent.Date:dd.MM.yyyy HH:mm}\n📍 {createdEvent.Location}\n" +
                            $"{(createdEvent.RegistrationDeadline.HasValue ? $"⏰ Дедлайн: {createdEvent.RegistrationDeadline:dd.MM.yyyy HH:mm}\n" : "")}" +
                            $"ID: `{createdEvent.Id}`", ParseMode.Markdown);
                        break;
                }
            }
            catch (Exception ex)
            {
                _createEventStates.Remove(chatId);
                await _botClient.SendTextMessageAsync(chatId, $"❌ Ошибка: {ex.Message}");
            }
        }
        private async Task HandleMyCompanyCommand(long chatId)
        {
            if (!CheckAuth(chatId, "CompanyManager")) return;

            try
            {
                var company = await _apiClient.GetMyCompanyAsync();
                await _botClient.SendTextMessageAsync(chatId,
                    $"🏢 *{company.Name}*\n\n{company.Description}\n\nID: `{company.Id}`",
                    ParseMode.Markdown);
            }
            catch (Exception ex)
            {
                await _botClient.SendTextMessageAsync(chatId, $"❌ Ошибка: {ex.Message}");
            }
        }
        private async Task HandleParticipantsCommand(long chatId, string[] parts)
        {
            if (!CheckAuth(chatId, "CompanyManager")) return;
            if (parts.Length < 2)
            {
                await _botClient.SendTextMessageAsync(chatId, "❌ Формат: /participants eventId");
                return;
            }

            try
            {
                if (!Guid.TryParse(parts[1], out var eventId))
                {
                    await _botClient.SendTextMessageAsync(chatId, "❌ Неверный формат ID мероприятия");
                    return;
                }

                var participants = await _apiClient.GetEventParticipantsAsync(eventId);
                if (participants.Count == 0)
                {
                    await _botClient.SendTextMessageAsync(chatId, "👥 На мероприятие еще никто не записался");
                    return;
                }

                var msg = "👥 *Участники мероприятия:*\n\n";
                foreach (var p in participants)
                {
                    msg += $"• {p.FirstName} {p.LastName} ({p.Email})\n";
                }

                await _botClient.SendTextMessageAsync(chatId, msg, ParseMode.Markdown);
            }
            catch (Exception ex)
            {
                await _botClient.SendTextMessageAsync(chatId, $"❌ Ошибка: {ex.Message}");
            }
        }
        private async Task HandleSetDeadlineCommand(long chatId, string[] parts)
        {
            if (!CheckAuth(chatId, "CompanyManager")) return;
            if (parts.Length < 3)
            {
                await _botClient.SendTextMessageAsync(chatId, "❌ Формат: /setdeadline eventId dd.MM.yyyy HH:mm");
                return;
            }

            try
            {
                if (!Guid.TryParse(parts[1], out var eventId))
                {
                    await _botClient.SendTextMessageAsync(chatId, "❌ Неверный формат ID мероприятия");
                    return;
                }

                if (!DateTime.TryParse(parts[2], out var deadline))
                {
                    await _botClient.SendTextMessageAsync(chatId, "❌ Неверный формат даты");
                    return;
                }

                var success = await _apiClient.SetEventDeadlineAsync(eventId, deadline);
                await _botClient.SendTextMessageAsync(chatId, success ?
                    $"✅ Дедлайн установлен на {deadline:dd.MM.yyyy HH:mm}" : "❌ Не удалось установить дедлайн");
            }
            catch (Exception ex)
            {
                await _botClient.SendTextMessageAsync(chatId, $"❌ Ошибка: {ex.Message}");
            }
        }
        private async Task HandleDeleteEventCommand(long chatId, string[] parts)
        {
            if (!CheckAuth(chatId, "CompanyManager")) return;
            if (parts.Length < 2)
            {
                await _botClient.SendTextMessageAsync(chatId, "❌ Формат: /deleteevent eventId");
                return;
            }

            try
            {
                if (!Guid.TryParse(parts[1], out var eventId))
                {
                    await _botClient.SendTextMessageAsync(chatId, "❌ Неверный формат ID мероприятия");
                    return;
                }

                var keyboard = new InlineKeyboardMarkup(new[]
                {
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("✅ Да", $"event_{eventId}_confirm"),
                        InlineKeyboardButton.WithCallbackData("❌ Нет", $"event_{eventId}_cancel")
                    }
                });

                await _botClient.SendTextMessageAsync(chatId,
                    "⚠️ Вы уверены, что хотите удалить мероприятие?",
                    replyMarkup: keyboard);
            }
            catch (Exception ex)
            {
                await _botClient.SendTextMessageAsync(chatId, $"❌ Ошибка: {ex.Message}");
            }
        }

        private async Task HandlePendingUsersCommand(long chatId)
        {
            if (!CheckAuth(chatId, "Deanery")) return;

            try
            {
                var users = await _apiClient.GetPendingUsersAsync();
                if (users.Count == 0)
                {
                    await _botClient.SendTextMessageAsync(chatId, "✅ Нет пользователей, ожидающих подтверждения");
                    return;
                }

                foreach (var user in users)
                {
                    var keyboard = new InlineKeyboardMarkup(new[]
                    {
                        new[] { InlineKeyboardButton.WithCallbackData("✅ Подтвердить", $"approve_{user.Id}") }
                    });

                    await _botClient.SendTextMessageAsync(chatId,
                        $"👤 *{user.FirstName} {user.LastName}*\n📧 {user.Email}\n🎭 {user.Role}\n🆔 `{user.Id}`",
                        ParseMode.Markdown, replyMarkup: keyboard);
                }
            }
            catch (Exception ex)
            {
                await _botClient.SendTextMessageAsync(chatId, $"❌ Ошибка: {ex.Message}");
            }
        }
        private async Task HandleApproveCommand(long chatId, string[] parts)
        {
            if (!CheckAuth(chatId, "Deanery")) return;
            if (parts.Length < 2)
            {
                await _botClient.SendTextMessageAsync(chatId, "❌ Формат: /approve userId");
                return;
            }

            try
            {
                var success = await _apiClient.ApproveUserAsync(parts[1]);
                await _botClient.SendTextMessageAsync(chatId, success ?
                    "✅ Пользователь подтвержден" : "❌ Не удалось подтвердить пользователя");
            }
            catch (Exception ex)
            {
                await _botClient.SendTextMessageAsync(chatId, $"❌ Ошибка: {ex.Message}");
            }
        }
        private async Task HandleCompaniesCommand(long chatId)
        {
            if (!CheckAuth(chatId, "Deanery")) return;

            try
            {
                var companies = await _apiClient.GetAllCompaniesAsync();
                if (companies.Count == 0)
                {
                    await _botClient.SendTextMessageAsync(chatId, "🏢 Нет зарегистрированных компаний");
                    return;
                }

                var msg = "🏢 *Все компании:*\n\n";
                foreach (var company in companies)
                {
                    msg += $"*{company.Name}*\n{company.Description}\n🆔 `{company.Id}`\n\n";
                }

                await _botClient.SendTextMessageAsync(chatId, msg, ParseMode.Markdown);
            }
            catch (Exception ex)
            {
                await _botClient.SendTextMessageAsync(chatId, $"❌ Ошибка: {ex.Message}");
            }
        }
        private async Task HandleAddManagerCommand(long chatId, string[] parts)
        {
            if (!CheckAuth(chatId, "Deanery")) return;
            if (parts.Length < 3)
            {
                await _botClient.SendTextMessageAsync(chatId, "❌ Формат: /addmanager companyId managerId");
                return;
            }

            try
            {
                if (!Guid.TryParse(parts[1], out var companyId))
                {
                    await _botClient.SendTextMessageAsync(chatId, "❌ Неверный формат ID компании");
                    return;
                }

                var success = await _apiClient.AddManagerToCompanyAsync(companyId, parts[2]);
                await _botClient.SendTextMessageAsync(chatId, success ?
                    "✅ Менеджер добавлен в компанию" : "❌ Не удалось добавить менеджера");
            }
            catch (Exception ex)
            {
                await _botClient.SendTextMessageAsync(chatId, $"❌ Ошибка: {ex.Message}");
            }
        }

        private async Task HandleEventsCommand(long chatId)
        {
            if (!CheckAuth(chatId)) return;

            try
            {
                var events = await _apiClient.GetEventsAsync(true);
                if (events.Count == 0)
                {
                    await _botClient.SendTextMessageAsync(chatId, "🎯 На данный момент нет мероприятий");
                    return;
                }

                var msg = "🎯 *Доступные мероприятия:*\n\n";
                foreach (var e in events)
                {
                    msg += $"*{e.Title}*\n📅 {e.Date:dd.MM.yyyy HH:mm}\n📍 {e.Location}\n" +
                          $"👥 Участников: {e.Participants?.Count ?? 0}\n" +
                          $"{(e.RegistrationDeadline.HasValue ? $"⏰ Дедлайн: {e.RegistrationDeadline:dd.MM.yyyy HH:mm}\n" : "")}" +
                          $"🆔 `{e.Id}`\n\n";
                }

                await _botClient.SendTextMessageAsync(chatId, msg, ParseMode.Markdown);
            }
            catch (Exception ex)
            {
                await _botClient.SendTextMessageAsync(chatId, $"❌ Ошибка: {ex.Message}");
            }
        }
        private async Task HandleMyEventsCommand(long chatId)
        {
            if (!CheckAuth(chatId)) return;

            try
            {
                var events = await _apiClient.GetUserEventsAsync();
                if (events.Count == 0)
                {
                    await _botClient.SendTextMessageAsync(chatId, "📝 Вы не записаны ни на одно мероприятие");
                    return;
                }

                var msg = "📝 *Мои мероприятия:*\n\n";
                foreach (var e in events)
                {
                    msg += $"*{e.Title}*\n📅 {e.Date:dd.MM.yyyy HH:mm}\n📍 {e.Location}\n" +
                          $"🏢 {e.CompanyName}\n👥 Участников: {e.Participants?.Count ?? 0}\n\n";
                }

                await _botClient.SendTextMessageAsync(chatId, msg, ParseMode.Markdown);
            }
            catch (Exception ex)
            {
                await _botClient.SendTextMessageAsync(chatId, $"❌ Ошибка: {ex.Message}");
            }
        }
        private bool CheckAuth(long chatId, string requiredRole = null)
        {
            var state = _userStateService.GetUserState(chatId);

            if (!state.IsAuthenticated)
            {
                _botClient.SendTextMessageAsync(chatId, "❌ Требуется авторизация. Используйте /login").Wait();
                return false;
            }

            if (!state.IsApproved)
            {
                _botClient.SendTextMessageAsync(chatId, "❌ Ваш аккаунт еще не подтвержден").Wait();
                return false;
            }

            if (requiredRole != null && state.UserRole != requiredRole)
            {
                _botClient.SendTextMessageAsync(chatId, $"❌ Команда доступна только для {requiredRole}").Wait();
                return false;
            }

            return true;
        }

        private async Task HandleApproveCallback(long chatId, string userId, int messageId) 
        {
            try
            {
                var success = await _apiClient.ApproveUserAsync(userId);

                if (success)
                {
                    await _botClient.DeleteMessageAsync(chatId, messageId);

                    await _botClient.SendTextMessageAsync(chatId, "✅ Пользователь подтвержден");
                }
                else
                {
                    await _botClient.SendTextMessageAsync(chatId, "❌ Не удалось подтвердить пользователя");
                }
            }
            catch (Exception ex)
            {
                await _botClient.SendTextMessageAsync(chatId, $"❌ Ошибка: {ex.Message}");
            }
        }

        private async Task HandleEventAction(long chatId, Guid eventId, string action)
        {
            try
            {
                if (action == "confirm")
                {
                    var success = await _apiClient.DeleteEventAsync(eventId);
                    await _botClient.SendTextMessageAsync(chatId, success ?
                        "✅ Мероприятие удалено" : "❌ Не удалось удалить мероприятие");
                }
                else
                {
                    await _botClient.SendTextMessageAsync(chatId, "❌ Удаление отменено");
                }
            }
            catch (Exception ex)
            {
                await _botClient.SendTextMessageAsync(chatId, $"❌ Ошибка: {ex.Message}");
            }
        }

        private async Task HandleEditEventCommand(long chatId, string[] parts)
        {
            await _botClient.SendTextMessageAsync(chatId, "✏️ Редактирование мероприятий будет доступно в следующих версиях");
        }

        private async Task HandleUnknownCommand(long chatId)
        {
            await _botClient.SendTextMessageAsync(chatId, "❌ Неизвестная команда. Используйте /help для справки");
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _cts.Cancel();
            return Task.CompletedTask;
        }
    }

    public class CreateEventState
    {
        public int Step { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime Date { get; set; }
        public string Location { get; set; }
        public DateTime? RegistrationDeadline { get; set; }
    }
}