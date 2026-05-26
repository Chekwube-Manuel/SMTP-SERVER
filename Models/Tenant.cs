using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EmailServer.Models
{
    public class Tenant
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        [Required]
        public string Name { get; set; } = string.Empty;
        [Required]
        public string Domain { get; set; } = string.Empty;
        [Required]
        public string ApiKey { get; set; } = string.Empty;
        public int MaxMessagesPerDay { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public ICollection<EmailMessage> Messages { get; set; } = new List<EmailMessage>();
        public ICollection<SendEvent> SendEvents { get; set; } = new List<SendEvent>();
    }
}
