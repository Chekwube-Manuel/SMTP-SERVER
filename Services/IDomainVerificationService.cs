using EmailServer.Models;

namespace EmailServer.Services
{
    public interface IDomainVerificationService
    {
        Task<DomainVerificationInfo?> GetVerificationInfoAsync(Guid tenantId);
        Task<bool> VerifyDomainAsync(Guid tenantId);
    }
}
