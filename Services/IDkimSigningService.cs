using EmailServer.Models;
using MimeKit;

namespace EmailServer.Services
{
    public interface IDkimSigningService
    {
        Task SignAsync(Tenant tenant, MimeMessage message);
    }
}
