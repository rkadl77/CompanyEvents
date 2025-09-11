using System.ComponentModel.DataAnnotations;

namespace HITS.Models.DTOs
{
    public class RejectUserDto
    {
        [Required]
        public string Reason { get; set; }
    }
}