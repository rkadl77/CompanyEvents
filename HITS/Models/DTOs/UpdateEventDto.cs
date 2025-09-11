namespace HITS.Models.DTOs
{
    public class UpdateEventDto
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
        public DateTime? Date { get; set; }
        public string? Location { get; set; }
        public DateTime? RegistrationDeadline { get; set; }
    }
}