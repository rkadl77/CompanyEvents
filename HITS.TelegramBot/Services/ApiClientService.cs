using HITS.Models.DTOs;
using Microsoft.Extensions.Configuration;
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace HITS.TelegramBot.Services
{
    public class ApiClientService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;

        public ApiClientService(IConfiguration configuration)
        {
            _httpClient = new HttpClient();
            _baseUrl = configuration["Api:BaseUrl"] ?? "https://localhost:7166";
        }

        public async Task<string> RegisterUserAsync(string email, string password, string firstName, string lastName, string role)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/api/Auth/register", new
                {
                    email,
                    password,
                    firstName,
                    lastName,
                    role
                });

                var content = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                    return "Регистрация успешна! Ожидайте подтверждения администратором.";

                if (content.Contains("DuplicateUserName") || content.Contains("DuplicateEmail"))
                    return $"Пользователь с email {email} уже существует. Используйте /login для входа.";

                return $"Ошибка регистрации: {content}";
            }
            catch (Exception ex)
            {
                return $"Ошибка подключения: {ex.Message}";
            }
        }

        public async Task<AuthResponse> LoginAsync(string email, string password)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/api/Auth/login", new
                {
                    email,
                    password
                });

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Ошибка входа: {errorContent}");
                }

                var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();

                if (authResponse == null)
                    throw new Exception("Неверный формат ответа от сервера");

                return authResponse;
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка при входе: {ex.Message}");
            }
        }
    }
}
