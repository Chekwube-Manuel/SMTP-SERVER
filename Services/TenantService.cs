using EmailServer.Data;
using EmailServer.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace EmailServer.Services
{
    public class TenantService : ITenantService
    {
        private readonly EmailServerContext _db;

        public TenantService(EmailServerContext db)
        {
            _db = db;
        }

        public async Task<Tenant> CreateTenantAsync(string name, string domain, int maxMessagesPerDay)
        {
            var (publicKey, privateKey) = GenerateDkimKeyPair();
            var tenant = new Tenant
            {
                Name = name,
                Domain = domain,
                ApiKey = GenerateApiKey(),
                VerificationToken = GenerateVerificationToken(),
                DkimSelector = "mail",
                DkimPublicKey = publicKey,
                DkimPrivateKey = privateKey,
                MaxMessagesPerDay = maxMessagesPerDay,
                CreatedAt = DateTime.UtcNow
            };

            _db.Tenants.Add(tenant);
            await _db.SaveChangesAsync();
            return tenant;
        }

        public Task<Tenant?> GetTenantAsync(Guid tenantId)
        {
            return _db.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId);
        }

        public Task<Tenant?> FindByApiKeyAsync(string apiKey)
        {
            return _db.Tenants.FirstOrDefaultAsync(t => t.ApiKey == apiKey);
        }

        public Task<IEnumerable<Tenant>> GetTenantsAsync()
        {
            return _db.Tenants.ToListAsync().ContinueWith(t => (IEnumerable<Tenant>)t.Result);
        }

        private static string GenerateApiKey()
        {
            return Convert.ToHexString(RandomNumberGenerator.GetBytes(24));
        }

        private static string GenerateVerificationToken()
        {
            return Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
        }

        private static (string publicKey, string privateKey) GenerateDkimKeyPair()
        {
            using var rsa = RSA.Create(2048);
            var publicKey = Convert.ToBase64String(rsa.ExportSubjectPublicKeyInfo());
            var privateKey = Convert.ToBase64String(rsa.ExportPkcs8PrivateKey());
            return (publicKey, privateKey);
        }
    }
}
