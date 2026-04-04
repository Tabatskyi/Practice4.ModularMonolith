using MediatR;
using Modules.Core.Application.API.Repos;
using Modules.Core.Domain;

namespace Modules.Core.Application.Commands;

public record GetListingByIdCommand(Guid Id) : IRequest<Listing?>;

public class GetListingByIdHandler(IListingRepository repo) : IRequestHandler<GetListingByIdCommand, Listing?>
{
    private readonly IListingRepository _repo = repo;

    public async Task<Listing?> Handle(GetListingByIdCommand command, CancellationToken cancellationToken)
    {
        return await _repo.Get(new ListingId(command.Id), cancellationToken);
    }
}