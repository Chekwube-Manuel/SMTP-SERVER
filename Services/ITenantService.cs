using EmailServer.Models;

namespace EmailServer.Services
{
    public interface ITenantService
    {
        Task<Tenant> CreateTenantAsync(string name, string domain, int maxMessagesPerDay);
        Task<Tenant?> GetTenantAsync(Guid tenantId);
        Task<Tenant?> FindByApiKeyAsync(string apiKey);
        Task<IEnumerable<Tenant>> GetTenantsAsync();
    }
}
