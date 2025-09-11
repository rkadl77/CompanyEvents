using HITS.Models.DTOs;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace HITS.TelegramBot.Services
{
    public class ApiClientService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private string _jwtToken;

        public ApiClientService(IConfiguration configuration)
        {
            _httpClient = new HttpClient();
            _baseUrl = configuration["Api:BaseUrl"] ?? "https://localhost:7166";
        }

        public void SetToken(string token)
        {
            _jwtToken = token;
            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }

        public string GetToken() => _jwtToken;
        public bool IsAuthenticated() => !string.IsNullOrEmpty(_jwtToken);

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
                var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/api/Auth/login", new { email, password });

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Ошибка входа: {errorContent}");
                }

                var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();
                if (authResponse == null)
                    throw new Exception("Неверный формат ответа от сервера");

                SetToken(authResponse.Token);
                return authResponse;
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка при входе: {ex.Message}");
            }
        }

        public async Task<UserDto> GetCurrentUserAsync()
        {
            EnsureAuthenticated();

            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/api/Auth/current-user");
                response.EnsureSuccessStatusCode();

                return await response.Content.ReadFromJsonAsync<UserDto>()!;
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка получения данных пользователя: {ex.Message}");
            }
        }

        public async Task<List<EventDto>> GetEventsAsync(bool upcomingOnly = true)
        {
            EnsureAuthenticated();

            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/api/Events?upcomingOnly={upcomingOnly}");
                response.EnsureSuccessStatusCode();

                return await response.Content.ReadFromJsonAsync<List<EventDto>>() ?? new List<EventDto>();
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка получения событий: {ex.Message}");
            }
        }

        public async Task<List<EventDto>> GetUserEventsAsync()
        {
            EnsureAuthenticated();

            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/api/Events/my-events");
                response.EnsureSuccessStatusCode();

                return await response.Content.ReadFromJsonAsync<List<EventDto>>() ?? new List<EventDto>();
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка получения моих событий: {ex.Message}");
            }
        }

        public async Task<EventDto> CreateEventAsync(CreateEventDto eventDto)
        {
            EnsureAuthenticated();

            try
            {
                var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/api/Events", eventDto);
                response.EnsureSuccessStatusCode();

                return await response.Content.ReadFromJsonAsync<EventDto>()!;
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка создания события: {ex.Message}");
            }
        }

        public async Task<bool> RegisterForEventAsync(Guid eventId)
        {
            EnsureAuthenticated();

            try
            {
                var response = await _httpClient.PostAsync($"{_baseUrl}/api/Events/{eventId}/register", null);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка регистрации на событие: {ex.Message}");
            }
        }

        public async Task<List<UserDto>> GetEventParticipantsAsync(Guid eventId)
        {
            EnsureAuthenticated();

            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/api/Events/{eventId}/participants");
                response.EnsureSuccessStatusCode();

                return await response.Content.ReadFromJsonAsync<List<UserDto>>() ?? new List<UserDto>();
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка получения участников: {ex.Message}");
            }
        }

        public async Task<List<UserDto>> GetPendingUsersAsync()
        {
            EnsureAuthenticated();

            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/api/Users/pending");
                response.EnsureSuccessStatusCode();

                return await response.Content.ReadFromJsonAsync<List<UserDto>>() ?? new List<UserDto>();
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка получения ожидающих пользователей: {ex.Message}");
            }
        }

        public async Task<bool> ApproveUserAsync(string userId)
        {
            EnsureAuthenticated();

            try
            {
                var response = await _httpClient.PostAsync($"{_baseUrl}/api/Users/{userId}/approve", null);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка подтверждения пользователя: {ex.Message}");
            }
        }

        public async Task<CompanyDto> GetMyCompanyAsync()
        {
            EnsureAuthenticated();

            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/api/Companies/my-company");
                response.EnsureSuccessStatusCode();

                return await response.Content.ReadFromJsonAsync<CompanyDto>()!;
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка получения компании: {ex.Message}");
            }
        }

        public async Task<CompanyDto> CreateCompanyAsync(CreateCompanyDto companyDto)
        {
            EnsureAuthenticated();

            try
            {
                var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/api/Companies", companyDto);
                response.EnsureSuccessStatusCode();

                return await response.Content.ReadFromJsonAsync<CompanyDto>()!;
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка создания компании: {ex.Message}");
            }
        }

        private void EnsureAuthenticated()
        {
            if (string.IsNullOrEmpty(_jwtToken))
                throw new Exception("Требуется авторизация. Выполните /login");
        }

        public void Logout()
        {
            _jwtToken = null;
            _httpClient.DefaultRequestHeaders.Authorization = null;
        }
    }
}
