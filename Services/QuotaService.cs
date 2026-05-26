using EmailServer.Data;
using EmailServer.Models;
using Microsoft.EntityFrameworkCore;

namespace EmailServer.Services
{
    public class QuotaService : IQuotaService
    {
        private readonly EmailServerContext _db;

        public QuotaService(EmailServerContext db)
        {
            _db = db;
        }

        public async Task<bool> CanSendAsync(Tenant tenant)
        {
            var today = DateTime.UtcNow.Date;
            var count = await _db.SendEvents
                .Where(e => e.TenantId == tenant.Id && e.SentAt >= today)
                .CountAsync();

            return count < tenant.MaxMessagesPerDay;
        }

        public async Task RecordSendAsync(Tenant tenant, EmailMessage message)
        {
            var events = message.To.Select(recipient => new SendEvent
            {
                TenantId = tenant.Id,
                Recipient = recipient,
                SentAt = DateTime.UtcNow
            });

            await _db.SendEvents.AddRangeAsync(events);
            await _db.SaveChangesAsync();
        }

        public async Task<object> GetUsageAsync(Tenant tenant)
        {
            var today = DateTime.UtcNow.Date;
            var sentToday = await _db.SendEvents
                .Where(e => e.TenantId == tenant.Id && e.SentAt >= today)
                .CountAsync();

            return new
            {
                tenant.Id,
                tenant.Name,
                tenant.Domain,
                tenant.MaxMessagesPerDay,
                SentToday = sentToday,
                Remaining = Math.Max(tenant.MaxMessagesPerDay - sentToday, 0)
            };
        }
    }
}
