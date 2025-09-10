namespace HITS.Models.DTOs
{
    public class CreateEventDto
    {
        public required string Title { get; set; }
        public required string Description { get; set; }
        public required DateTime Date { get; set; }
        public required string Location { get; set; }
        public required DateTime? RegistrationDeadline { get; set; }
    }
}