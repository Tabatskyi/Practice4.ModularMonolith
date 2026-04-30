using MediatR;
using Modules.Core.Application.API.Repos;
using Modules.Core.Application.API.Users;
using Modules.Core.Domain;

namespace Modules.Core.Application.Commands;

public record CreateListingCommand(string Title, decimal Price, Guid OwnerUserId) : IRequest<Guid>;

public class CreateListingHandler(IListingRepository repo, IUsersServiceClient usersServiceClient) : IRequestHandler<CreateListingCommand, Guid>
{
    private readonly IListingRepository _repo = repo;
    private readonly IUsersServiceClient _usersServiceClient = usersServiceClient;

    public async Task<Guid> Handle(CreateListingCommand command, CancellationToken cancellationToken)
    {
        await _usersServiceClient.EnsureUserExists(command.OwnerUserId, cancellationToken);

        var listing = new Listing(ListingId.New(), command.OwnerUserId, command.Title, command.Price);
        await _repo.Create(listing, cancellationToken);
        return listing.Id.Value;
    }
}