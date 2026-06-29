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
            var normalizedDomain = NormalizeDomain(domain);
            var (publicKey, privateKey) = GenerateDkimKeyPair();
            var verificationToken = GenerateVerificationToken();

            var tenant = new Tenant
            {
                Name = name,
                Domain = normalizedDomain,
                ApiKey = GenerateApiKey(),
                VerificationToken = verificationToken,
                DkimSelector = "mail",
                DkimPublicKey = publicKey,
                DkimPrivateKey = privateKey,
                MaxMessagesPerDay = maxMessagesPerDay,
                CreatedAt = DateTime.UtcNow
            };

            tenant.Domains.Add(new TenantDomain
            {
                TenantId = tenant.Id,
                Domain = normalizedDomain,
                IsPrimary = true,
                VerificationToken = verificationToken,
                DkimSelector = tenant.DkimSelector,
                DkimPublicKey = publicKey,
                DkimPrivateKey = privateKey,
                CreatedAt = tenant.CreatedAt
            });

            _db.Tenants.Add(tenant);
            await _db.SaveChangesAsync();
            return tenant;
        }

        public Task<Tenant?> GetTenantAsync(Guid tenantId)
        {
            return _db.Tenants.Include(tenant => tenant.Domains).FirstOrDefaultAsync(t => t.Id == tenantId);
        }

        public Task<Tenant?> FindByApiKeyAsync(string apiKey)
        {
            return _db.Tenants.Include(tenant => tenant.Domains).FirstOrDefaultAsync(t => t.ApiKey == apiKey);
        }

        public async Task<IEnumerable<Tenant>> GetTenantsAsync()
        {
            return await _db.Tenants.Include(tenant => tenant.Domains).ToListAsync();
        }

        public async Task<TenantDomain?> AddDomainAsync(Guid tenantId, string domain)
        {
            var tenant = await _db.Tenants.Include(item => item.Domains).FirstOrDefaultAsync(item => item.Id == tenantId);
            if (tenant is null)
            {
                return null;
            }

            var normalizedDomain = NormalizeDomain(domain);
            var existingDomain = await _db.TenantDomains.FirstOrDefaultAsync(item => item.Domain == normalizedDomain);
            if (existingDomain is not null)
            {
                throw new InvalidOperationException($"Domain {normalizedDomain} is already registered.");
            }

            var (publicKey, privateKey) = GenerateDkimKeyPair();
            var tenantDomain = new TenantDomain
            {
                TenantId = tenant.Id,
                Domain = normalizedDomain,
                IsPrimary = tenant.Domains.Count == 0,
                VerificationToken = GenerateVerificationToken(),
                DkimSelector = "mail",
                DkimPublicKey = publicKey,
                DkimPrivateKey = privateKey,
                CreatedAt = DateTime.UtcNow
            };

            _db.TenantDomains.Add(tenantDomain);
            await _db.SaveChangesAsync();
            return tenantDomain;
        }

        public async Task<List<TenantDomain>> GetDomainsAsync(Guid tenantId)
        {
            return await _db.TenantDomains
                .Where(domain => domain.TenantId == tenantId)
                .OrderByDescending(domain => domain.IsPrimary)
                .ThenBy(domain => domain.Domain)
                .ToListAsync();
        }

        public Task<TenantDomain?> GetDomainAsync(Guid tenantId, string domain)
        {
            var normalizedDomain = NormalizeDomain(domain);
            return _db.TenantDomains.FirstOrDefaultAsync(item => item.TenantId == tenantId && item.Domain == normalizedDomain);
        }

        public async Task<TenantDomain?> FindSendingDomainAsync(Guid tenantId, string fromDomain)
        {
            var normalizedDomain = NormalizeDomain(fromDomain);
            var domains = await _db.TenantDomains
                .Where(domain => domain.TenantId == tenantId)
                .ToListAsync();

            return domains
                .Where(domain => DomainMatches(normalizedDomain, domain.Domain))
                .OrderByDescending(domain => domain.Domain.Length)
                .FirstOrDefault();
        }

        public static bool DomainMatches(string fromDomain, string ownedDomain)
        {
            return string.Equals(fromDomain, ownedDomain, StringComparison.OrdinalIgnoreCase) ||
                fromDomain.EndsWith($".{ownedDomain}", StringComparison.OrdinalIgnoreCase);
        }

        public static string NormalizeDomain(string domain)
        {
            return domain.Trim().TrimEnd('.').ToLowerInvariant();
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
