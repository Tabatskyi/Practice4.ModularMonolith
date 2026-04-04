namespace Modules.Core.Domain;

public sealed class Listing
{
    public ListingId Id { get; }

    public string Title { get; private set; }

    public decimal Price { get; private set; }

    public ListingStatus Status { get; private set; }

    public DateTime CreatedAtUtc { get; }

    public Listing(ListingId id, string title, decimal price, ListingStatus status = ListingStatus.Draft, DateTime? createdAtUtc = null)
    {
        Id = id;
        Title = ValidateTitle(title);
        Price = ValidatePrice(price);
        Status = ListingStatus.Draft;
        CreatedAtUtc = EnsureUtc(createdAtUtc ?? DateTime.UtcNow);
        ApplyStatus(status);
    }

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

    private static DateTime EnsureUtc(DateTime dateTime) =>
        dateTime.Kind == DateTimeKind.Utc
            ? dateTime
            : DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);

    private void ApplyStatus(ListingStatus status)
    {
        switch (status)
        {
            case ListingStatus.Draft:
                break;
            case ListingStatus.Published:
                ChangeStatus(ListingStatus.Published);
                break;
            case ListingStatus.Sold:
                ChangeStatus(ListingStatus.Published);
                ChangeStatus(ListingStatus.Sold);
                break;
            case ListingStatus.Deleted:
                ChangeStatus(ListingStatus.Deleted);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(status), status, "Unknown listing status.");
        }
    }
}