using Microsoft.EntityFrameworkCore;
using Modules.Core.Infrastructure.Persistence.Entities;

namespace Modules.Core.Infrastructure.Persistence;

public sealed class ListingDbContext(DbContextOptions<ListingDbContext> options) : DbContext(options)
{
    public DbSet<ListingEntity> Listings => Set<ListingEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ListingEntity>(builder =>
        {
            builder.ToTable("listings");
            builder.HasKey(entity => entity.Id);
            builder.Property(entity => entity.Id)
                .ValueGeneratedNever();
            builder.Property(entity => entity.Title)
                .HasMaxLength(256)
                .IsRequired();
            builder.Property(entity => entity.Price)
                .HasColumnType("numeric(18,2)")
                .IsRequired();
            builder.Property(entity => entity.Status)
                .HasConversion<string>()
                .HasMaxLength(32)
                .IsRequired();
            builder.Property(entity => entity.CreatedAtUtc)
                .IsRequired();
        });

        base.OnModelCreating(modelBuilder);
    }
}