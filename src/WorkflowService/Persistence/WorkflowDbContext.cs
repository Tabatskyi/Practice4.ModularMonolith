using Microsoft.EntityFrameworkCore;

namespace WorkflowService.Persistence;

public sealed class WorkflowDbContext(DbContextOptions<WorkflowDbContext> options) : DbContext(options)
{
    public DbSet<WorkflowInstanceEntity> WorkflowInstances => Set<WorkflowInstanceEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WorkflowInstanceEntity>(builder =>
        {
            builder.ToTable("workflow_instances");

            builder.HasKey(entity => entity.WorkflowId);

            builder.Property(entity => entity.WorkflowId)
                .HasColumnName("workflow_id")
                .ValueGeneratedNever();

            builder.Property(entity => entity.Type)
                .HasColumnName("type")
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(entity => entity.State)
                .HasColumnName("state")
                .HasMaxLength(64)
                .IsRequired();

            builder.Property(entity => entity.CreatedAt)
                .HasColumnName("created_at")
                .IsRequired();

            builder.Property(entity => entity.UpdatedAt)
                .HasColumnName("updated_at")
                .IsRequired();

            builder.Property(entity => entity.LastError)
                .HasColumnName("last_error")
                .HasMaxLength(2048);
        });

        base.OnModelCreating(modelBuilder);
    }
}
