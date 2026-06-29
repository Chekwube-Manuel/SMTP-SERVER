using EmailServer.Models;

namespace EmailServer.Services
{
    public interface ITenantService
    {
        Task<Tenant> CreateTenantAsync(string name, string domain, int maxMessagesPerDay);
        Task<Tenant?> GetTenantAsync(Guid tenantId);
        Task<Tenant?> FindByApiKeyAsync(string apiKey);
        Task<IEnumerable<Tenant>> GetTenantsAsync();
        Task<TenantDomain?> AddDomainAsync(Guid tenantId, string domain);
        Task<List<TenantDomain>> GetDomainsAsync(Guid tenantId);
        Task<TenantDomain?> GetDomainAsync(Guid tenantId, string domain);
        Task<TenantDomain?> FindSendingDomainAsync(Guid tenantId, string fromDomain);
    }
}
