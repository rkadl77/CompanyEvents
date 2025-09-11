using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;

namespace HITS.Models.Entities
{
    public class User : IdentityUser
    {
        [Required]
        public string FirstName { get; set; }
        [Required]
        public string LastName { get; set; }
        [Required]
        public string Role { get; set; } 
        public bool IsApproved { get; set; } = false;
        public Guid? CompanyId { get; set; }
        public virtual Company Company { get; set; }
        public string? TelegramChatId { get; set; }

        public virtual ICollection<Event> ParticipatedEvents { get; set; } = new List<Event>();

        public string? RejectionReason { get; set; }
    }
}