using Modules.Core.Application.API.Repos;
using Modules.Core.Application.API.Users;
using Modules.Core.Application.Commands;
using Modules.Core.Domain;

namespace Modules.Core.Tests.Application;

public class CreateCoreHandlerTests
{
    [Fact]
    public async Task Handle_PersistsListing_AndReturnsCreatedId()
    {
        var repository = new InMemoryListingRepository();
        var ownerUserId = Guid.NewGuid();
        var usersServiceClient = new FakeUsersServiceClient(ownerUserId);
        var handler = new CreateListingHandler(repository, usersServiceClient);
        var command = new CreateListingCommand("Intel Core i5-9400F", 1200, ownerUserId);

        var createdId = await handler.Handle(command, CancellationToken.None);
        var createdListing = await repository.Get(new ListingId(createdId), CancellationToken.None);

        Assert.NotEqual(Guid.Empty, createdId);
        Assert.NotNull(createdListing);
        Assert.Equal("Intel Core i5-9400F", createdListing!.Title);
        Assert.Equal(1200, createdListing.Price);
        Assert.Equal(ownerUserId, createdListing.OwnerUserId);
        Assert.Equal(ListingStatus.Draft, createdListing.Status);
    }

    [Fact]
    public async Task Handle_WhenOwnerUserDoesNotExist_ThrowsUserNotFoundException()
    {
        var repository = new InMemoryListingRepository();
        var handler = new CreateListingHandler(repository, new FakeUsersServiceClient());
        var command = new CreateListingCommand("Intel Core i5-9400F", 1200, Guid.NewGuid());

        await Assert.ThrowsAsync<UserNotFoundException>(() => handler.Handle(command, CancellationToken.None));
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

    private sealed class FakeUsersServiceClient(params Guid[] existingUserIds) : IUsersServiceClient
    {
        private readonly HashSet<Guid> _existingUserIds = existingUserIds.ToHashSet();

        public Task EnsureUserExists(Guid userId, CancellationToken cancellationToken = default)
        {
            if (!_existingUserIds.Contains(userId))
            {
                throw new UserNotFoundException(userId);
            }

            return Task.CompletedTask;
        }
    }
}