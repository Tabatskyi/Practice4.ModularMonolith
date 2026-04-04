namespace Modules.Core.Domain;

public sealed class Listing(ListingId id, string title, decimal price)
{
    public ListingId Id { get; } = id;

    public string Title { get; private set; } = ValidateTitle(title);

    public decimal Price { get; private set; } = ValidatePrice(price);

    public ListingStatus Status { get; private set; } = ListingStatus.Draft;

    public DateTime CreatedAtUtc { get; } = DateTime.UtcNow;

    public void ChangeStatus(ListingStatus newStatus)
    {
        if (Status == newStatus)
        {
            return;
        }

        if (!IsValidTransition(Status, newStatus))
        {
            throw new InvalidOperationException($"Cannot transition status from {Status} to {newStatus}.");
        }
        Status = newStatus;
    }

    public void UpdateDetails(string title, decimal price)
    {
        if (Status == ListingStatus.Sold)
        {
            throw new InvalidOperationException("Sold listings cannot be updated.");
        }

        Title = ValidateTitle(title);
        Price = ValidatePrice(price);
    }

    private static bool IsValidTransition(ListingStatus currentStatus, ListingStatus newStatus) =>
        (currentStatus, newStatus) switch
        {
            (ListingStatus.Draft, ListingStatus.Published) => true,
            (ListingStatus.Published, ListingStatus.Sold) => true,
            (ListingStatus.Published, ListingStatus.Draft) => true,
            (ListingStatus.Draft, ListingStatus.Deleted) => true,
            _ => false
        };

    private static string ValidateTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Title cannot be empty.", nameof(title));
        }

        return title.Trim();
    }

    private static decimal ValidatePrice(decimal price)
    {
        if (price <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(price),
                "Price must be greater than zero.");
        }

        return price;
    }
}