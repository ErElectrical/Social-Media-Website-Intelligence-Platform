
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
    public DbSet<WebsiteCollection> WebsiteCollections =>
    Set<WebsiteCollection>();

    public DbSet<WebsiteCollectionChange> WebsiteCollectionChanges =>
    Set<WebsiteCollectionChange>();

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

        modelBuilder.Entity<WebsiteCollection>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.PageUrl)
                .HasMaxLength(2048)
                .IsRequired();

            entity.Property(x => x.PageTitle)
                .HasMaxLength(500);

            entity.Property(x => x.PageType)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(x => x.ExtractedDataJson)
                .HasColumnType("longtext");

            entity.Property(x => x.ContentHash)
                .HasMaxLength(64);

            entity.Property(x => x.ErrorMessage)
                .HasColumnType("text");

            entity.HasOne<MonitoredSource>()
                .WithMany()
                .HasForeignKey(x => x.MonitoredSourceId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(x => x.MonitoredSourceId);
            entity.HasIndex(x => x.ContentHash);
        });

        modelBuilder.Entity<WebsiteCollectionChange>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Field)
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(x => x.ChangeType)
                .HasMaxLength(20)
                .IsRequired();

            // Old and new values may contain long website paragraphs.
            entity.Property(x => x.OldValue)
                .HasColumnType("longtext");

            entity.Property(x => x.NewValue)
                .HasColumnType("longtext");

            entity.HasOne<WebsiteCollection>()
                .WithMany()
                .HasForeignKey(x => x.WebsiteCollectionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(x => x.WebsiteCollectionId);
        });
    }
}
