namespace WorkflowService.Clients;

public enum CoreListingStatus
{
    Draft,
    Published,
    Sold
}

public interface ICoreServiceClient
{
    Task UpdateListingStatusAsync(Guid listingId, CoreListingStatus status, CancellationToken cancellationToken = default);
}
