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
            var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId);
            if (tenant is null)
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(tenant.VerificationToken))
            {
                InitializeVerificationData(tenant);
                await _db.SaveChangesAsync();
            }

            return BuildVerificationInfo(tenant);
        }

        public async Task<bool> VerifyDomainAsync(Guid tenantId)
        {
            var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId);
            if (tenant is null)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(tenant.VerificationToken))
            {
                InitializeVerificationData(tenant);
                await _db.SaveChangesAsync();
            }

            var recordName = $"_verify.{tenant.Domain}";
            IReadOnlyCollection<string> txtRecords;

            try
            {
                var response = await _dnsClient.QueryAsync(recordName, QueryType.TXT);
                txtRecords = response.Answers
                    .TxtRecords()
                    .Select(record => string.Concat(record.Text))
                    .ToList();
            }
            catch (DnsResponseException)
            {
                return false;
            }

            if (!txtRecords.Any(record => string.Equals(record.Trim(), tenant.VerificationToken, StringComparison.Ordinal)))
            {
                return false;
            }

            tenant.DomainVerified = true;
            tenant.DomainVerifiedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return true;
        }

        private static void InitializeVerificationData(Tenant tenant)
        {
            tenant.VerificationToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
            tenant.DkimSelector = "mail";

            using var rsa = RSA.Create(2048);
            var publicKey = Convert.ToBase64String(rsa.ExportSubjectPublicKeyInfo());
            tenant.DkimPublicKey = publicKey;
            tenant.DkimPrivateKey = Convert.ToBase64String(rsa.ExportPkcs8PrivateKey());
        }

        private static DomainVerificationInfo BuildVerificationInfo(Tenant tenant)
        {
            var verificationName = $"_verify.{tenant.Domain}";
            var spfName = tenant.Domain;
            var dkimName = $"{tenant.DkimSelector}._domainkey.{tenant.Domain}";

            return new DomainVerificationInfo
            {
                Domain = tenant.Domain,
                Verified = tenant.DomainVerified,
                VerifiedAt = tenant.DomainVerifiedAt,
                VerificationRecordName = verificationName,
                VerificationRecordValue = tenant.VerificationToken,
                SpfRecordName = spfName,
                SpfRecordValue = $"v=spf1 mx include:spf.{tenant.Domain} -all",
                DkimRecordName = dkimName,
                DkimRecordValue = $"v=DKIM1; k=rsa; p={tenant.DkimPublicKey}"
            };
        }
    }
}
