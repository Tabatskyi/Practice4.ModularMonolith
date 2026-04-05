namespace Modules.Core.Domain;

public record ListingId
{
    public Guid Value { get; }

    public ListingId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Listing item id cannot be empty.", nameof(value));
        }

        Value = value;
    }

    public static ListingId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}