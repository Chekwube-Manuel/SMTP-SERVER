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
        public DbSet<TenantDomain> TenantDomains => Set<TenantDomain>();
        public DbSet<EmailMessage> EmailMessages => Set<EmailMessage>();
        public DbSet<QueuedEmail> QueuedEmails => Set<QueuedEmail>();
        public DbSet<SendEvent> SendEvents => Set<SendEvent>();
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TenantDomain>()
                .HasIndex(domain => new { domain.TenantId, domain.Domain })
                .IsUnique();

            modelBuilder.Entity<TenantDomain>()
                .HasIndex(domain => domain.Domain);
        }
    }
}

