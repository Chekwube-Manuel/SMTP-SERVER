using EmailServer.Models;

namespace EmailServer.Services
{
    public interface IQuotaService
    {
        Task<bool> CanSendAsync(Tenant tenant);
        Task RecordSendAsync(Tenant tenant, EmailMessage message);
        Task<object> GetUsageAsync(Tenant tenant);
    }
}
