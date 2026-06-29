using System.Text;
using EmailServer.Models;
using Microsoft.Extensions.Options;
using MimeKit;
using MimeKit.Cryptography;

namespace EmailServer.Services
{
    public class DkimSigningService : IDkimSigningService
    {
        private readonly DkimOptions _options;
        private readonly ITenantService _tenantService;
        private readonly ILogger<DkimSigningService> _logger;

        public DkimSigningService(
            IOptions<DkimOptions> options,
            ITenantService tenantService,
            ILogger<DkimSigningService> logger)
        {
            _options = options.Value;
            _tenantService = tenantService;
            _logger = logger;
        }

        public async Task SignAsync(Tenant tenant, MimeMessage message)
        {
            if (!_options.Enabled || message.Headers.Contains(HeaderId.DkimSignature))
            {
                return;
            }

            var fromDomain = message.From.Mailboxes.FirstOrDefault()?.Domain;
            if (string.IsNullOrWhiteSpace(fromDomain))
            {
                _logger.LogWarning("Skipping DKIM signing because the message has no mailbox From domain.");
                return;
            }

            var signingDomain = await _tenantService.FindSendingDomainAsync(tenant.Id, fromDomain);
            if (signingDomain is null)
            {
                _logger.LogWarning(
                    "Skipping DKIM signing because From domain {FromDomain} is not registered for tenant {TenantId}.",
                    fromDomain,
                    tenant.Id);
                return;
            }

            if (_options.RequireVerifiedDomain && !signingDomain.Verified)
            {
                _logger.LogWarning("Skipping DKIM signing for unverified domain {Domain}.", signingDomain.Domain);
                return;
            }

            if (string.IsNullOrWhiteSpace(signingDomain.DkimSelector) || string.IsNullOrWhiteSpace(signingDomain.DkimPrivateKey))
            {
                _logger.LogWarning("Tenant domain {DomainId} has no DKIM selector or private key configured.", signingDomain.Id);
                return;
            }

            try
            {
                using var privateKey = new MemoryStream(Encoding.ASCII.GetBytes(ToPemPrivateKey(signingDomain.DkimPrivateKey)));
                var signer = new DkimSigner(privateKey, signingDomain.Domain, signingDomain.DkimSelector, DkimSignatureAlgorithm.RsaSha256)
                {
                    HeaderCanonicalizationAlgorithm = DkimCanonicalizationAlgorithm.Relaxed,
                    BodyCanonicalizationAlgorithm = DkimCanonicalizationAlgorithm.Relaxed,
                    AgentOrUserIdentifier = $"@{signingDomain.Domain}"
                };

                if (_options.SignatureExpirationHours > 0)
                {
                    signer.SignaturesExpireAfter = TimeSpan.FromHours(_options.SignatureExpirationHours);
                }

                signer.Sign(message, GetHeadersToSign());
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Failed to DKIM sign message for tenant {TenantId} and domain {Domain}.",
                    tenant.Id,
                    signingDomain.Domain);
            }
        }

        private List<string> GetHeadersToSign()
        {
            var headers = _options.HeadersToSign
                .Where(header => !string.IsNullOrWhiteSpace(header))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            return headers.Count > 0 ? headers : ["From", "To", "Subject", "Date"];
        }

        private static string ToPemPrivateKey(string base64PrivateKey)
        {
            var trimmed = base64PrivateKey.Trim();
            if (trimmed.StartsWith("-----BEGIN", StringComparison.Ordinal))
            {
                return trimmed;
            }

            var body = string.Join(Environment.NewLine, SplitEvery(trimmed, 64));
            return $"-----BEGIN PRIVATE KEY-----{Environment.NewLine}{body}{Environment.NewLine}-----END PRIVATE KEY-----{Environment.NewLine}";
        }

        private static IEnumerable<string> SplitEvery(string value, int length)
        {
            for (var i = 0; i < value.Length; i += length)
            {
                yield return value.Substring(i, Math.Min(length, value.Length - i));
            }
        }
    }
}
