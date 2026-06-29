using EmailServer.Models;

namespace EmailServer.Services
{
    public interface IDomainVerificationService
    {
        Task<DomainVerificationInfo?> GetVerificationInfoAsync(Guid tenantId);
        Task<DomainVerificationInfo?> GetVerificationInfoAsync(Guid tenantId, string domainName);
        Task<DomainAuthenticationStatus?> GetAuthenticationStatusAsync(Guid tenantId);
        Task<DomainAuthenticationStatus?> GetAuthenticationStatusAsync(Guid tenantId, string domainName);
        Task<bool> VerifyDomainAsync(Guid tenantId);
        Task<bool> VerifyDomainAsync(Guid tenantId, string domainName);
    }
}
