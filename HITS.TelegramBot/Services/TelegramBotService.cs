using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using HITS.Models.DTOs;

namespace HITS.TelegramBot.Services
{
    public class TelegramBotService : IHostedService
    {
        private readonly TelegramBotClient _botClient;
        private readonly ILogger<TelegramBotService> _logger;
        private readonly ApiClientService _apiClient;
        private readonly SimpleMappingService _mappingService;
        private CancellationTokenSource _cts;

        public TelegramBotService(IConfiguration configuration,
                                  ILogger<TelegramBotService> logger,
                                  ApiClientService apiClient,
                                  SimpleMappingService mappingService)
        {
            _logger = logger;
            _apiClient = apiClient;
            _mappingService = mappingService;
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
            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "Добро пожаловать в HITS System! 🎓\n\n" +
                      "Доступные команды:\n" +
                      "/register email password firstName lastName role - регистрация\n" +
                      "/login email password - вход\n" +
                      "/events - все мероприятия\n" +
                      "/myevents - мои мероприятия");
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

                    string userId = authResponse.User.Id;
                    string userRole = authResponse.User.Role;
                    bool isApproved = authResponse.User.IsApproved;

                    _mappingService.AddMapping(userId, chatId);

                    await _botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: $"✅ Вход выполнен!\nID: {userId}\nРоль: {userRole}\nСтатус: {(isApproved ? "Подтвержден" : "Ожидает подтверждения")}");
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
                        string userId = loginResult.User.Id;
                        _mappingService.AddMapping(userId, chatId);

                        await _botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: registrationResult + $"\nВаш ID: {userId}");
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
            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "Функция просмотра мероприятий в разработке");
        }

        private async Task HandleMyEventsCommand(long chatId)
        {
            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "Функция просмотра моих мероприятий в разработке");
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
