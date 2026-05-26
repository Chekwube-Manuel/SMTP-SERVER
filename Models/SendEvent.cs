using System.ComponentModel.DataAnnotations;

namespace EmailServer.Models
{
    public class SendEvent
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid TenantId { get; set; }
        public DateTime SentAt { get; set; } = DateTime.UtcNow;
        public string Recipient { get; set; } = string.Empty;
        public Tenant? Tenant { get; set; }
    }
}
