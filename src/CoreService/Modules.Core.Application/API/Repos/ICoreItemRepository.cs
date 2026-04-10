using Modules.Core.Domain;

namespace Modules.Core.Application.API.Repos;

public interface IListingRepository
{
    Task<Listing?> Get(ListingId id, CancellationToken cancellationToken = default);

    Task Create(Listing entity, CancellationToken cancellationToken = default);

    Task Save(Listing entity, CancellationToken cancellationToken = default);

    Task<bool> Delete(ListingId id, CancellationToken cancellationToken = default);
}