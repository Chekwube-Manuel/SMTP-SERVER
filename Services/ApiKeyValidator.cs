using EmailServer.Models;
using EmailServer.Services;

namespace EmailServer.Services
{
    public class ApiKeyValidator : IApiKeyValidator
    {
        private readonly ITenantService _tenantService;

        public ApiKeyValidator(ITenantService tenantService)
        {
            _tenantService = tenantService;
        }

        public Task<Tenant?> ValidateAsync(string apiKey)
        {
            return _tenantService.FindByApiKeyAsync(apiKey);
        }
    }
}
