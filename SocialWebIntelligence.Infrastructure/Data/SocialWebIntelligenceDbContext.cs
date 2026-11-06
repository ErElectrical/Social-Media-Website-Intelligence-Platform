
using Microsoft.EntityFrameworkCore;
using SocialWebIntelligence.Core.Entities;

namespace SocialWebIntelligence.Infrastructure.Data;

public class SocialWebIntelligenceDbContext : DbContext
{
    public SocialWebIntelligenceDbContext(
        DbContextOptions<SocialWebIntelligenceDbContext> options)
        : base(options)
    {
    }

    public DbSet<MonitoredSource> MonitoredSources =>
        Set<MonitoredSource>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<MonitoredSource>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.CompanyName)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(x => x.Url)
                .HasMaxLength(2048)
                .IsRequired();

            entity.Property(x => x.Platform)
                .HasConversion<string>()
                .HasMaxLength(30);

            entity.HasIndex(x => new
            {
                x.CompanyName,
                x.Platform,
                x.Url
            }).IsUnique();
        });
    }
}
