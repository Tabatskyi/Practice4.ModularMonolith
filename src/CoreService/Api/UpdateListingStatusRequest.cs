using Modules.Core.Domain;

namespace Api;

public record UpdateListingStatusRequest(ListingStatus NewStatus);
