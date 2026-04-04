using Modules.Core.Domain;

namespace Modules.Core.Infrastructure.Persistence.Entities;

public sealed class ListingEntity
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public ListingStatus Status { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}