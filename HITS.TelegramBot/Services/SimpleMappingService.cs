using System.Collections.Generic;

namespace HITS.TelegramBot.Services
{
    public class SimpleMappingService
    {
        private readonly Dictionary<string, long> _userChatMapping = new();

        public void AddMapping(string userId, long chatId)
        {
            _userChatMapping[userId] = chatId;
            Console.WriteLine($"Added mapping: UserId={userId} -> ChatId={chatId}");
        }

        public long? GetChatId(string userId)
        {
            return _userChatMapping.ContainsKey(userId) ? _userChatMapping[userId] : null;
        }

        public void RemoveMapping(string userId)
        {
            if (_userChatMapping.ContainsKey(userId))
            {
                _userChatMapping.Remove(userId);
            }
        }
    }
}