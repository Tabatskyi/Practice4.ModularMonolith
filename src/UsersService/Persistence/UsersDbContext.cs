using Microsoft.EntityFrameworkCore;
using UsersService.Persistence.Entities;

namespace UsersService.Persistence;

public sealed class UsersDbContext(DbContextOptions<UsersDbContext> options) : DbContext(options)
{
    public DbSet<UserEntity> Users => Set<UserEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserEntity>(builder =>
        {
            builder.ToTable("users");

            builder.HasKey(entity => entity.UserId);

            builder.Property(entity => entity.UserId)
                .ValueGeneratedNever();

            builder.Property(entity => entity.DisplayName)
                .HasMaxLength(128)
                .IsRequired();
        });

        base.OnModelCreating(modelBuilder);
    }
}