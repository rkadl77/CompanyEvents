using Microsoft.Extensions.Logging;

namespace HITS.Models.Entities
{
    public class Company
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; }
        public string Description { get; set; }

        public virtual ICollection<User> Managers { get; set; } = new List<User>();
        public virtual ICollection<Event> Events { get; set; } = new List<Event>();
    }
}