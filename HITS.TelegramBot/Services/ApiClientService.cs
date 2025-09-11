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
            var response = await _httpClient.GetAsync($"{_baseUrl}/api/Auth/current-user");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<UserDto>();
        }

        public async Task<List<EventDto>> GetEventsAsync(bool upcomingOnly = true)
        {
            EnsureAuthenticated();
            var response = await _httpClient.GetAsync($"{_baseUrl}/api/Events?upcomingOnly={upcomingOnly}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<List<EventDto>>() ?? new List<EventDto>();
        }

        public async Task<List<EventDto>> GetUserEventsAsync()
        {
            EnsureAuthenticated();
            var response = await _httpClient.GetAsync($"{_baseUrl}/api/Events/my-events");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<List<EventDto>>() ?? new List<EventDto>();
        }

        public async Task<EventDto> GetEventAsync(Guid eventId)
        {
            EnsureAuthenticated();
            var response = await _httpClient.GetAsync($"{_baseUrl}/api/Events/{eventId}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<EventDto>();
        }

        public async Task<EventDto> CreateEventAsync(CreateEventDto eventDto)
        {
            EnsureAuthenticated();
            var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/api/Events", eventDto);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<EventDto>();
        }

        public async Task<bool> UpdateEventAsync(Guid eventId, UpdateEventDto eventDto)
        {
            EnsureAuthenticated();
            var response = await _httpClient.PutAsJsonAsync($"{_baseUrl}/api/Events/{eventId}", eventDto);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> DeleteEventAsync(Guid eventId)
        {
            EnsureAuthenticated();
            var response = await _httpClient.DeleteAsync($"{_baseUrl}/api/Events/{eventId}");
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> RegisterForEventAsync(Guid eventId)
        {
            EnsureAuthenticated();
            var response = await _httpClient.PostAsync($"{_baseUrl}/api/Events/{eventId}/register", null);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> UnregisterFromEventAsync(Guid eventId)
        {
            EnsureAuthenticated();
            var response = await _httpClient.DeleteAsync($"{_baseUrl}/api/Events/{eventId}/register");
            return response.IsSuccessStatusCode;
        }

        public async Task<List<UserDto>> GetEventParticipantsAsync(Guid eventId)
        {
            EnsureAuthenticated();
            var response = await _httpClient.GetAsync($"{_baseUrl}/api/Events/{eventId}/participants");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<List<UserDto>>() ?? new List<UserDto>();
        }

        public async Task<bool> SetEventDeadlineAsync(Guid eventId, DateTime deadline)
        {
            EnsureAuthenticated();
            var updateDto = new UpdateEventDto { RegistrationDeadline = deadline };
            var response = await _httpClient.PutAsJsonAsync($"{_baseUrl}/api/Events/{eventId}", updateDto);
            return response.IsSuccessStatusCode;
        }

        public async Task<List<UserDto>> GetPendingUsersAsync()
        {
            EnsureAuthenticated();
            var response = await _httpClient.GetAsync($"{_baseUrl}/api/Users/pending");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<List<UserDto>>() ?? new List<UserDto>();
        }

        public async Task<bool> ApproveUserAsync(string userId)
        {
            EnsureAuthenticated();
            var response = await _httpClient.PostAsync($"{_baseUrl}/api/Users/{userId}/approve", null);
            return response.IsSuccessStatusCode;
        }

        public async Task<CompanyDto> GetMyCompanyAsync()
        {
            EnsureAuthenticated();
            var response = await _httpClient.GetAsync($"{_baseUrl}/api/Companies/my-company");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<CompanyDto>();
        }

        public async Task<List<CompanyDto>> GetAllCompaniesAsync()
        {
            EnsureAuthenticated();
            var response = await _httpClient.GetAsync($"{_baseUrl}/api/Companies");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<List<CompanyDto>>() ?? new List<CompanyDto>();
        }

        public async Task<CompanyDto> CreateCompanyAsync(CreateCompanyDto companyDto)
        {
            EnsureAuthenticated();
            var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/api/Companies", companyDto);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<CompanyDto>();
        }

        public async Task<bool> AddManagerToCompanyAsync(Guid companyId, string managerId)
        {
            EnsureAuthenticated();
            var response = await _httpClient.PostAsync($"{_baseUrl}/api/Companies/{companyId}/managers/{managerId}", null);
            return response.IsSuccessStatusCode;
        }

        public async Task<List<UserDto>> GetCompanyManagersAsync(Guid companyId)
        {
            EnsureAuthenticated();
            var response = await _httpClient.GetAsync($"{_baseUrl}/api/Companies/{companyId}/managers");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<List<UserDto>>() ?? new List<UserDto>();
        }

        public async Task<bool> RemoveManagerFromCompanyAsync(Guid companyId, string managerId)
        {
            EnsureAuthenticated();
            var response = await _httpClient.DeleteAsync($"{_baseUrl}/api/Companies/{companyId}/managers/{managerId}");
            return response.IsSuccessStatusCode;
        }

        public async Task<string> GetGoogleAuthUrlAsync()
        {
            EnsureAuthenticated();
            var response = await _httpClient.GetAsync($"{_baseUrl}/api/GoogleAuth/auth-url");
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync();
        }

        public async Task<bool> CheckGoogleCalendarAccessAsync()
        {
            EnsureAuthenticated();
            var response = await _httpClient.GetAsync($"{_baseUrl}/api/GoogleAuth/status");
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            return content.Contains("\"hasAccess\":true") || content.Contains("true");
        }

        public async Task<bool> TestAddEventToCalendarAsync()
        {
            EnsureAuthenticated();
            var response = await _httpClient.GetAsync($"{_baseUrl}/api/GoogleAuth/test-add-event");
            return response.IsSuccessStatusCode;
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
        public async Task<List<EventDto>> GetManagerEventsAsync()
        {
            EnsureAuthenticated();
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/api/Events/manager-events");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<List<EventDto>>() ?? new List<EventDto>();
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка получения мероприятий компании: {ex.Message}");
            }
        }

        public async Task<bool> RejectUserAsync(string userId, string reason)
        {
            EnsureAuthenticated();
            try
            {
                var rejectDto = new { Reason = reason };
                var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/api/Users/{userId}/reject", rejectDto);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка отклонения пользователя: {ex.Message}");
            }
        }
    }
}