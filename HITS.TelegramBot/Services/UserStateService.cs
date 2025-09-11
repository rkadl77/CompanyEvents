using HITS.Models.DTOs;
using System.Collections.Concurrent;

namespace HITS.TelegramBot.Services
{
    public class UserStateService
    {
        private readonly ConcurrentDictionary<long, UserState> _userStates = new();

        public void SetUserState(long chatId, UserState state)
        {
            _userStates[chatId] = state;
        }

        public UserState GetUserState(long chatId)
        {
            return _userStates.ContainsKey(chatId) ? _userStates[chatId] : new UserState();
        }

        public void ClearUserState(long chatId)
        {
            _userStates.TryRemove(chatId, out _);
        }
    }

    public class UserState
    {
        public string UserId { get; set; }
        public string UserRole { get; set; }
        public bool IsApproved { get; set; }
        public string JwtToken { get; set; }

        public bool IsAuthenticated => !string.IsNullOrEmpty(UserId) && !string.IsNullOrEmpty(JwtToken);
        public bool CanAccessEvents => IsAuthenticated && IsApproved;
    }
}