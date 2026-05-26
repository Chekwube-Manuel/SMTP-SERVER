using EmailServer.Models;
using Microsoft.EntityFrameworkCore;

namespace EmailServer.Data
{
    public class EmailServerContext : DbContext
    {
        public EmailServerContext(DbContextOptions<EmailServerContext> options)
            : base(options)
        {
        }

        public DbSet<Tenant> Tenants => Set<Tenant>();
        public DbSet<EmailMessage> EmailMessages => Set<EmailMessage>();
        public DbSet<SendEvent> SendEvents => Set<SendEvent>();
    }
}
