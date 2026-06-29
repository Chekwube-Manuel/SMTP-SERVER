using DnsClient;
using EmailServer.Data;
using EmailServer.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace EmailServer.Services
{
    public class DomainVerificationService : IDomainVerificationService
    {
        private readonly EmailServerContext _db;
        private readonly LookupClient _dnsClient;

        public DomainVerificationService(EmailServerContext db)
        {
            _db = db;
            _dnsClient = new LookupClient();
        }

        public async Task<DomainVerificationInfo?> GetVerificationInfoAsync(Guid tenantId)
        {
            var domain = await GetPrimaryDomainAsync(tenantId);
            return domain is null ? null : BuildVerificationInfo(domain);
        }

        public async Task<DomainVerificationInfo?> GetVerificationInfoAsync(Guid tenantId, string domainName)
        {
            var domain = await GetTenantDomainAsync(tenantId, domainName);
            return domain is null ? null : BuildVerificationInfo(domain);
        }

        public async Task<bool> VerifyDomainAsync(Guid tenantId)
        {
            var domain = await GetPrimaryDomainAsync(tenantId);
            return domain is not null && await VerifyDomainAsync(domain);
        }

        public async Task<bool> VerifyDomainAsync(Guid tenantId, string domainName)
        {
            var domain = await GetTenantDomainAsync(tenantId, domainName);
            return domain is not null && await VerifyDomainAsync(domain);
        }

        public async Task<DomainAuthenticationStatus?> GetAuthenticationStatusAsync(Guid tenantId)
        {
            var domain = await GetPrimaryDomainAsync(tenantId);
            return domain is null ? null : await GetAuthenticationStatusAsync(domain);
        }

        public async Task<DomainAuthenticationStatus?> GetAuthenticationStatusAsync(Guid tenantId, string domainName)
        {
            var domain = await GetTenantDomainAsync(tenantId, domainName);
            return domain is null ? null : await GetAuthenticationStatusAsync(domain);
        }

        private async Task<bool> VerifyDomainAsync(TenantDomain domain)
        {
            EnsureVerificationData(domain);
            var verification = await CheckTxtRecordAsync($"_verify.{domain.Domain}", domain.VerificationToken);
            if (!verification.Found)
            {
                return false;
            }

            domain.Verified = true;
            domain.VerifiedAt = DateTime.UtcNow;
            await SyncPrimaryTenantFieldsAsync(domain);
            await _db.SaveChangesAsync();
            return true;
        }

        private async Task<DomainAuthenticationStatus> GetAuthenticationStatusAsync(TenantDomain domain)
        {
            EnsureVerificationData(domain);
            var info = BuildVerificationInfo(domain);
            var status = new DomainAuthenticationStatus
            {
                Domain = domain.Domain,
                DomainVerified = domain.Verified,
                DomainVerifiedAt = domain.VerifiedAt,
                Verification = await CheckTxtRecordAsync(info.VerificationRecordName, info.VerificationRecordValue),
                Spf = await CheckTxtRecordAsync(info.SpfRecordName, info.SpfRecordValue),
                Dkim = await CheckTxtRecordAsync(info.DkimRecordName, info.DkimRecordValue),
                Dmarc = await CheckTxtRecordAsync(info.DmarcRecordName, info.DmarcRecordValue)
            };

            if (!domain.Verified && status.Verification.Found)
            {
                domain.Verified = true;
                domain.VerifiedAt = DateTime.UtcNow;
                await SyncPrimaryTenantFieldsAsync(domain);
                await _db.SaveChangesAsync();

                status.DomainVerified = true;
                status.DomainVerifiedAt = domain.VerifiedAt;
            }

            status.ReadyToSendDirect =
                status.DomainVerified &&
                status.Spf.Found &&
                status.Dkim.Found &&
                status.Dmarc.Found;

            return status;
        }

        private async Task<TenantDomain?> GetPrimaryDomainAsync(Guid tenantId)
        {
            var domain = await _db.TenantDomains
                .Where(item => item.TenantId == tenantId)
                .OrderByDescending(item => item.IsPrimary)
                .ThenBy(item => item.CreatedAt)
                .FirstOrDefaultAsync();

            if (domain is not null)
            {
                return domain;
            }

            var tenant = await _db.Tenants.FirstOrDefaultAsync(item => item.Id == tenantId);
            if (tenant is null)
            {
                return null;
            }

            domain = BuildDomainFromLegacyTenant(tenant, true);
            _db.TenantDomains.Add(domain);
            await _db.SaveChangesAsync();
            return domain;
        }

        private Task<TenantDomain?> GetTenantDomainAsync(Guid tenantId, string domainName)
        {
            var normalizedDomain = TenantService.NormalizeDomain(domainName);
            return _db.TenantDomains.FirstOrDefaultAsync(item => item.TenantId == tenantId && item.Domain == normalizedDomain);
        }

        private async Task SyncPrimaryTenantFieldsAsync(TenantDomain domain)
        {
            if (!domain.IsPrimary)
            {
                return;
            }

            var tenant = await _db.Tenants.FirstOrDefaultAsync(item => item.Id == domain.TenantId);
            if (tenant is null)
            {
                return;
            }

            tenant.Domain = domain.Domain;
            tenant.DomainVerified = domain.Verified;
            tenant.DomainVerifiedAt = domain.VerifiedAt;
            tenant.VerificationToken = domain.VerificationToken;
            tenant.DkimSelector = domain.DkimSelector;
            tenant.DkimPublicKey = domain.DkimPublicKey;
            tenant.DkimPrivateKey = domain.DkimPrivateKey;
        }

        private static TenantDomain BuildDomainFromLegacyTenant(Tenant tenant, bool isPrimary)
        {
            var domain = new TenantDomain
            {
                TenantId = tenant.Id,
                Domain = TenantService.NormalizeDomain(tenant.Domain),
                IsPrimary = isPrimary,
                Verified = tenant.DomainVerified,
                VerifiedAt = tenant.DomainVerifiedAt,
                VerificationToken = tenant.VerificationToken,
                DkimSelector = string.IsNullOrWhiteSpace(tenant.DkimSelector) ? "mail" : tenant.DkimSelector,
                DkimPublicKey = tenant.DkimPublicKey,
                DkimPrivateKey = tenant.DkimPrivateKey,
                CreatedAt = tenant.CreatedAt
            };

            EnsureVerificationData(domain);
            return domain;
        }

        private static void EnsureVerificationData(TenantDomain domain)
        {
            if (string.IsNullOrWhiteSpace(domain.VerificationToken))
            {
                domain.VerificationToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
            }

            if (string.IsNullOrWhiteSpace(domain.DkimSelector))
            {
                domain.DkimSelector = "mail";
            }

            if (!string.IsNullOrWhiteSpace(domain.DkimPublicKey) && !string.IsNullOrWhiteSpace(domain.DkimPrivateKey))
            {
                return;
            }

            using var rsa = RSA.Create(2048);
            domain.DkimPublicKey = Convert.ToBase64String(rsa.ExportSubjectPublicKeyInfo());
            domain.DkimPrivateKey = Convert.ToBase64String(rsa.ExportPkcs8PrivateKey());
        }

        private static DomainVerificationInfo BuildVerificationInfo(TenantDomain domain)
        {
            var verificationName = $"_verify.{domain.Domain}";
            var spfName = domain.Domain;
            var dkimName = $"{domain.DkimSelector}._domainkey.{domain.Domain}";

            return new DomainVerificationInfo
            {
                Domain = domain.Domain,
                Verified = domain.Verified,
                VerifiedAt = domain.VerifiedAt,
                VerificationRecordName = verificationName,
                VerificationRecordValue = domain.VerificationToken,
                SpfRecordName = spfName,
                SpfRecordValue = "v=spf1 mx a -all",
                DkimRecordName = dkimName,
                DkimRecordValue = $"v=DKIM1; k=rsa; p={domain.DkimPublicKey}",
                DmarcRecordName = $"_dmarc.{domain.Domain}",
                DmarcRecordValue = "v=DMARC1; p=quarantine; adkim=s; aspf=s"
            };
        }

        private async Task<DnsRecordCheck> CheckTxtRecordAsync(string name, string expectedValue)
        {
            var check = new DnsRecordCheck
            {
                Name = name,
                ExpectedValue = expectedValue
            };

            try
            {
                var response = await _dnsClient.QueryAsync(name, QueryType.TXT);
                check.ActualValues = response.Answers
                    .TxtRecords()
                    .Select(record => string.Concat(record.Text).Trim())
                    .Where(record => !string.IsNullOrWhiteSpace(record))
                    .ToList();

                check.Found = check.ActualValues.Any(record =>
                    string.Equals(NormalizeTxt(record), NormalizeTxt(expectedValue), StringComparison.Ordinal));
            }
            catch (DnsResponseException ex)
            {
                check.Error = ex.Message;
            }

            return check;
        }

        private static string NormalizeTxt(string value)
        {
            return string.Join(' ', value.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        }
    }
}
