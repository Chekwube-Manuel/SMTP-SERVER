using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EmailServer.Models
{
    public class EmailMessage
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid TenantId { get; set; }
        [Required]
        public string From { get; set; } = string.Empty;
        public string ToSerialized { get; set; } = string.Empty;
        [NotMapped]
        public List<string> To
        {
            get => string.IsNullOrWhiteSpace(ToSerialized)
                ? new List<string>()
                : ToSerialized.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList();
            set => ToSerialized = string.Join(';', value);
        }
        [Required]
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public Tenant? Tenant { get; set; }
    }
}
