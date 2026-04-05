using Modules.Core.Application.API.Repos;
using Modules.Core.Application.Commands;
using Modules.Core.Domain;

namespace Modules.Core.Tests.Application;

public class CreateCoreHandlerTests
{
    [Fact]
    public async Task Handle_PersistsListing_AndReturnsCreatedId()
    {
        var repository = new InMemoryListingRepository();
        var handler = new CreateListingHandler(repository);
        var command = new CreateListingCommand("Intel Core i5-9400F", 1200);

        var createdId = await handler.Handle(command, CancellationToken.None);
        var createdListing = await repository.Get(new ListingId(createdId), CancellationToken.None);

        Assert.NotEqual(Guid.Empty, createdId);
        Assert.NotNull(createdListing);
        Assert.Equal("Intel Core i5-9400F", createdListing!.Title);
        Assert.Equal(1200, createdListing.Price);
        Assert.Equal(ListingStatus.Draft, createdListing.Status);
    }

    private sealed class InMemoryListingRepository : IListingRepository
    {
        private readonly Dictionary<Guid, Listing> _storage = new();

        public Task<Listing?> Get(ListingId id, CancellationToken cancellationToken = default)
        {
            _storage.TryGetValue(id.Value, out var listing);
            return Task.FromResult(listing);
        }

        public Task Create(Listing entity, CancellationToken cancellationToken = default)
        {
            _storage[entity.Id.Value] = entity;
            return Task.CompletedTask;
        }

        public Task Save(Listing entity, CancellationToken cancellationToken = default)
        {
            _storage[entity.Id.Value] = entity;
            return Task.CompletedTask;
        }

        public Task<bool> Delete(ListingId id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_storage.Remove(id.Value));
        }
    }
}