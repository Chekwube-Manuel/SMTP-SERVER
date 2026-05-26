using EmailServer.Models;

namespace EmailServer.Services
{
    public interface IApiKeyValidator
    {
        Task<Tenant?> ValidateAsync(string apiKey);
    }
}
