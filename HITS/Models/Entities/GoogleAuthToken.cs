using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HITS.Models.Entities
{
    public class GoogleAuthToken
    {
        [Key]
        public string UserId { get; set; }

        [ForeignKey("UserId")]
        public User User { get; set; }

        public string AccessToken { get; set; }
        public string? RefreshToken { get; set; } 
        public DateTime ExpiryDate { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}