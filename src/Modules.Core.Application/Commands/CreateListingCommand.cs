using MediatR;
using Modules.Core.Application.API.Repos;
using Modules.Core.Domain;

namespace Modules.Core.Application.Commands;

public record CreateListingCommand(string Title, decimal Price) : IRequest<Guid>;

public class CreateListingHandler(IListingRepository repo) : IRequestHandler<CreateListingCommand, Guid>
{
    private readonly IListingRepository _repo = repo;

    public async Task<Guid> Handle(CreateListingCommand command, CancellationToken cancellationToken)
    {
        var listing = new Listing(ListingId.New(), command.Title, command.Price);
        await _repo.Create(listing, cancellationToken);
        return listing.Id.Value;
    }
}