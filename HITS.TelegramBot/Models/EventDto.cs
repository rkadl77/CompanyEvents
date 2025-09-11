using System;
using System.Collections.Generic;

namespace HITS.TelegramBot.Models
{
    public class EventDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string Location { get; set; } = string.Empty;
        public DateTime? RegistrationDeadline { get; set; }
        public CompanyDto? Company { get; set; }
        public List<UserDto>? Participants { get; set; }
    }

    public class CompanyDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class UserDto
    {
        public Guid Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }
}
