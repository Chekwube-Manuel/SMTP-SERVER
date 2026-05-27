using EmailServer.Models;

namespace EmailServer.Services
{
    public interface IQuotaService
    {
        Task<bool> CanSendAsync(Tenant tenant, int recipientCount);
        Task RecordSendAsync(Tenant tenant, EmailMessage message);
        Task<object> GetUsageAsync(Tenant tenant);
    }
}
