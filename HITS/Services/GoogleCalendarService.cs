using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using HITS.Data;
using HITS.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace HITS.Services
{
    public class GoogleCalendarService : IGoogleCalendarService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly string[] _scopes = { CalendarService.Scope.CalendarEvents };
        private readonly string _applicationName = "HITS System";

        public GoogleCalendarService(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public async Task<string> GetAuthUrlAsync(string userId, string redirectUri)
        {
            var clientSecrets = new ClientSecrets
            {
                ClientId = _configuration["GoogleCalendar:ClientId"],
                ClientSecret = _configuration["GoogleCalendar:ClientSecret"]
            };

            var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = clientSecrets,
                Scopes = _scopes
            });

            var url = flow.CreateAuthorizationCodeRequest(redirectUri);
            url.State = userId;
            return url.Build().AbsoluteUri;
        }
        public async Task<bool> SaveTokensAsync(string userId, string code, string redirectUri)
        {
            try
            {
                Console.WriteLine($"=== SaveTokensAsync Started ===");
                Console.WriteLine($"UserId: {userId}");
                Console.WriteLine($"Code length: {code?.Length}");
                Console.WriteLine($"RedirectUri: {redirectUri}");

                var clientSecrets = new ClientSecrets
                {
                    ClientId = _configuration["GoogleCalendar:ClientId"],
                    ClientSecret = _configuration["GoogleCalendar:ClientSecret"]
                };

                Console.WriteLine($"ClientId: {clientSecrets.ClientId}");
                Console.WriteLine($"ClientSecret present: {!string.IsNullOrEmpty(clientSecrets.ClientSecret)}");

                var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
                {
                    ClientSecrets = clientSecrets,
                    Scopes = _scopes
                });

                Console.WriteLine("Exchanging code for token...");
                var token = await flow.ExchangeCodeForTokenAsync(
                    userId, code, redirectUri, CancellationToken.None);

                Console.WriteLine($"Token received. AccessToken length: {token.AccessToken?.Length}");
                Console.WriteLine($"RefreshToken: {!string.IsNullOrEmpty(token.RefreshToken)}");
                Console.WriteLine($"ExpiresIn: {token.ExpiresInSeconds} seconds");

                double expiresInSeconds = token.ExpiresInSeconds ?? 3600;

                var existingToken = await _context.GoogleAuthTokens.FindAsync(userId);
                if (existingToken != null)
                {
                    Console.WriteLine("Updating existing token...");
                    existingToken.AccessToken = token.AccessToken;
                    existingToken.RefreshToken = token.RefreshToken;
                    existingToken.ExpiryDate = DateTime.UtcNow.AddSeconds(expiresInSeconds);
                }
                else
                {
                    Console.WriteLine("Creating new token...");
                    _context.GoogleAuthTokens.Add(new GoogleAuthToken
                    {
                        UserId = userId,
                        AccessToken = token.AccessToken,
                        RefreshToken = token.RefreshToken,
                        ExpiryDate = DateTime.UtcNow.AddSeconds(expiresInSeconds)
                    });
                }

                await _context.SaveChangesAsync();
                Console.WriteLine("Tokens saved successfully!");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"=== ERROR in SaveTokensAsync ===");
                Console.WriteLine($"Message: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                }
                return false;
            }
        }

        public async Task<bool> AddEventToCalendarAsync(string userId, HITS.Models.Entities.Event eventObj)
        {
            try
            {
                var token = await _context.GoogleAuthTokens.FindAsync(userId);
                if (token == null || token.ExpiryDate < DateTime.UtcNow)
                {
                    if (!await RefreshTokenAsync(userId))
                        return false;
                }

                var service = await GetCalendarServiceAsync(userId);

                var calendarEvent = new Google.Apis.Calendar.v3.Data.Event
                {
                    Summary = eventObj.Title,
                    Description = eventObj.Description,
                    Location = eventObj.Location,
                    Start = new EventDateTime
                    {
                        DateTime = eventObj.Date,
                        TimeZone = "Europe/Moscow"
                    },
                    End = new EventDateTime
                    {
                        DateTime = eventObj.Date.AddHours(2),
                        TimeZone = "Europe/Moscow"
                    },
                    Reminders = new Google.Apis.Calendar.v3.Data.Event.RemindersData
                    {
                        UseDefault = true
                    }
                };

                var createdEvent = await service.Events.Insert(calendarEvent, "primary").ExecuteAsync();
                return createdEvent != null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding event to calendar: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> RefreshTokenAsync(string userId)
        {
            try
            {
                var token = await _context.GoogleAuthTokens.FindAsync(userId);
                if (token == null || string.IsNullOrEmpty(token.RefreshToken))
                    return false;

                var clientSecrets = new ClientSecrets
                {
                    ClientId = _configuration["GoogleCalendar:ClientId"],
                    ClientSecret = _configuration["GoogleCalendar:ClientSecret"]
                };

                var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
                {
                    ClientSecrets = clientSecrets,
                    Scopes = _scopes
                });

                var newToken = await flow.RefreshTokenAsync(userId, token.RefreshToken, CancellationToken.None);

                if (newToken != null)
                {
                    double expiresInSeconds = newToken.ExpiresInSeconds ?? 3600;

                    token.AccessToken = newToken.AccessToken;
                    token.ExpiryDate = DateTime.UtcNow.AddSeconds(expiresInSeconds);
                    await _context.SaveChangesAsync();
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error refreshing token: {ex.Message}");
            }
            return false;
        }

        private async Task<CalendarService> GetCalendarServiceAsync(string userId)
        {
            var token = await _context.GoogleAuthTokens.FindAsync(userId);
            if (token == null)
                throw new Exception("User not authenticated with Google Calendar");

            var expiresInSeconds = (token.ExpiryDate - DateTime.UtcNow).TotalSeconds;
            if (expiresInSeconds < 0) expiresInSeconds = 3600; 

            var tokenResponse = new TokenResponse
            {
                AccessToken = token.AccessToken,
                RefreshToken = token.RefreshToken,
                ExpiresInSeconds = (long)expiresInSeconds
            };

            var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = new ClientSecrets
                {
                    ClientId = _configuration["GoogleCalendar:ClientId"],
                    ClientSecret = _configuration["GoogleCalendar:ClientSecret"]
                },
                Scopes = _scopes
            });

            var credential = new UserCredential(flow, userId, tokenResponse);

            return new CalendarService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = _applicationName
            });
        }

        public async Task<bool> HasCalendarAccessAsync(string userId)
        {
            var token = await _context.GoogleAuthTokens.FindAsync(userId);
            return token != null && token.ExpiryDate > DateTime.UtcNow.AddMinutes(5);
        }

        public async Task<bool> RemoveEventFromCalendarAsync(string userId, string calendarEventId)
        {
            try
            {
                var service = await GetCalendarServiceAsync(userId);
                await service.Events.Delete("primary", calendarEventId).ExecuteAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error removing event from calendar: {ex.Message}");
                return false;
            }
        }
    }
}