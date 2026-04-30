namespace Api;

public record CreateListingRequest(string Title, decimal Price, Guid OwnerUserId);
