using Modules.Core.Domain;

namespace Api;

public record ListingResponse(Guid Id, string Title, decimal Price, ListingStatus Status, DateTime CreatedAtUtc)
{
	public static ListingResponse FromDomain(Listing listing) =>
		new(listing.Id.Value, listing.Title, listing.Price, listing.Status, listing.CreatedAtUtc);
}
