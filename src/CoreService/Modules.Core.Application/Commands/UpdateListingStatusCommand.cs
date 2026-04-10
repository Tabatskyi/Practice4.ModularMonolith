using MediatR;
using Modules.Core.Application.API.Repos;
using Modules.Core.Domain;

namespace Modules.Core.Application.Commands;

public record UpdateListingStatusCommand(Guid Id, ListingStatus NewStatus) : IRequest<Listing?>;

public class UpdateListingStatusHandler(IListingRepository repo) : IRequestHandler<UpdateListingStatusCommand, Listing?>
{
    private readonly IListingRepository _repo = repo;

    public async Task<Listing?> Handle(UpdateListingStatusCommand command, CancellationToken cancellationToken)
    {
        var listingId = new ListingId(command.Id);
        var listing = await _repo.Get(listingId, cancellationToken);

        if (listing is null)
        {
            return null;
        }

        listing.ChangeStatus(command.NewStatus);
        await _repo.Save(listing, cancellationToken);

        return listing;
    }
}