using System.Buffers;
using System.Text;
using EmailServer.Data;
using EmailServer.Models;
using SmtpServer;
using SmtpServer.Protocol;
using SmtpServer.Storage;

namespace EmailServer.Services
{
    public class QueuedEmailMessageStore : MessageStore
    {
        private const string TenantIdProperty = "TenantId";
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<QueuedEmailMessageStore> _logger;

        public QueuedEmailMessageStore(IServiceScopeFactory scopeFactory, ILogger<QueuedEmailMessageStore> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public override async Task<SmtpResponse> SaveAsync(
            ISessionContext context,
            IMessageTransaction transaction,
            ReadOnlySequence<byte> buffer,
            CancellationToken cancellationToken)
        {
            if (!context.Properties.TryGetValue(TenantIdProperty, out var tenantIdValue) || tenantIdValue is not Guid tenantId)
            {
                return SmtpResponse.AuthenticationRequired;
            }

            var from = $"{transaction.From.User}@{transaction.From.Host}";
            if (string.IsNullOrWhiteSpace(transaction.From.Host))
            {
                return SmtpResponse.MailboxNameNotAllowed;
            }

            using var scope = _scopeFactory.CreateScope();
            var tenantService = scope.ServiceProvider.GetRequiredService<ITenantService>();
            var sendingDomain = await tenantService.FindSendingDomainAsync(tenantId, transaction.From.Host);
            if (sendingDomain is not { Verified: true })
            {
                _logger.LogWarning(
                    "Rejected SMTP message for tenant {TenantId}: From domain {FromDomain} is not registered and verified.",
                    tenantId,
                    transaction.From.Host);

                return SmtpResponse.MailboxNameNotAllowed;
            }

            var recipients = transaction.To
                .Select(mailbox => $"{mailbox.User}@{mailbox.Host}")
                .Where(recipient => !string.IsNullOrWhiteSpace(recipient))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (recipients.Count == 0)
            {
                return SmtpResponse.NoValidRecipientsGiven;
            }

            var queuedEmail = new QueuedEmail
            {
                TenantId = tenantId,
                From = from,
                RecipientsSerialized = string.Join(';', recipients),
                RawMessage = ReadMessage(buffer),
                Status = QueuedEmailStatus.Queued,
                CreatedAt = DateTime.UtcNow
            };

            var db = scope.ServiceProvider.GetRequiredService<EmailServerContext>();
            await db.QueuedEmails.AddAsync(queuedEmail, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Queued SMTP message {MessageId} for tenant {TenantId} with {RecipientCount} recipient(s)",
                queuedEmail.Id,
                tenantId,
                recipients.Count);

            return SmtpResponse.Ok;
        }

        private static string ReadMessage(ReadOnlySequence<byte> buffer)
        {
            if (buffer.IsSingleSegment)
            {
                return Encoding.UTF8.GetString(buffer.FirstSpan);
            }

            var message = new StringBuilder((int)buffer.Length);
            foreach (var segment in buffer)
            {
                message.Append(Encoding.UTF8.GetString(segment.Span));
            }

            return message.ToString();
        }
    }
}
