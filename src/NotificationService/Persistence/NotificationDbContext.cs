using Microsoft.EntityFrameworkCore;
using NotificationService.Persistence.Entities;

namespace NotificationService.Persistence;

public sealed class NotificationDbContext(DbContextOptions<NotificationDbContext> options) : DbContext(options)
{
    public DbSet<NotificationEntity> Notifications => Set<NotificationEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<NotificationEntity>(builder =>
        {
            builder.ToTable("notifications");

            builder.HasKey(entity => entity.EventId);

            builder.Property(entity => entity.EventId)
                .HasColumnName("event_id")
                .ValueGeneratedNever();

            builder.Property(entity => entity.CorrelationId)
                .HasColumnName("correlation_id")
                .HasMaxLength(128)
                .IsRequired();

            builder.Property(entity => entity.Payload)
                .HasColumnName("payload")
                .HasColumnType("jsonb")
                .IsRequired();

            builder.Property(entity => entity.CreatedAt)
                .HasColumnName("created_at")
                .HasColumnType("timestamp with time zone")
                .IsRequired();
        });

        base.OnModelCreating(modelBuilder);
    }
}