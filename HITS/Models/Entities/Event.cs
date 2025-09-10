namespace HITS.Models.Entities
{
    public class Event
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Title { get; set; }
        public string? Description { get; set; }
        public DateTime Date { get; set; }
        public string Location { get; set; }
        public DateTime? RegistrationDeadline { get; set; }
        public Guid CompanyId { get; set; }
        public virtual Company Company { get; set; }

        public virtual ICollection<User> Participants { get; set; } = new List<User>();

        public string? GoogleCalendarEventId { get; set; } 
    }
}