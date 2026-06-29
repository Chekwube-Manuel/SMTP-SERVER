using System.ComponentModel.DataAnnotations;

namespace EmailServer.Models
{
    public class TenantDomain
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid TenantId { get; set; }
        [Required]
        public string Domain { get; set; } = string.Empty;
        public bool IsPrimary { get; set; }
        public bool Verified { get; set; }
        public DateTime? VerifiedAt { get; set; }
        public string VerificationToken { get; set; } = string.Empty;
        public string DkimSelector { get; set; } = "mail";
        public string DkimPublicKey { get; set; } = string.Empty;
        public string DkimPrivateKey { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public Tenant? Tenant { get; set; }
    }
}
