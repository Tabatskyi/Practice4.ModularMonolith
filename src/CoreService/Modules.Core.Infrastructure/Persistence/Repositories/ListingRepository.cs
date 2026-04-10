using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Modules.Core.Application.API.Repos;
using Modules.Core.Domain;
using Modules.Core.Infrastructure.Persistence.Entities;

namespace Modules.Core.Infrastructure.Persistence.Repositories;

public sealed class ListingRepository(ListingDbContext dbContext, IMapper mapper) : IListingRepository
{
    private readonly ListingDbContext _dbContext = dbContext;
    private readonly IMapper _mapper = mapper;

    public async Task<Listing?> Get(ListingId id, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.Listings.AsNoTracking().SingleOrDefaultAsync(entity => entity.Id == id.Value, cancellationToken);

        return entity is null ? null : _mapper.Map<Listing>(entity);
    }

    public async Task Create(Listing listing, CancellationToken cancellationToken = default)
    {
        await _dbContext.Listings.AddAsync(_mapper.Map<ListingEntity>(listing), cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task Save(Listing listing, CancellationToken cancellationToken = default)
    {
        var tracked = await _dbContext.Listings.SingleOrDefaultAsync(entity => entity.Id == listing.Id.Value, cancellationToken) ?? throw new InvalidOperationException($"Listing with id {listing.Id} was not found.");
        _mapper.Map(listing, tracked);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> Delete(ListingId id, CancellationToken cancellationToken = default)
    {
        var tracked = await _dbContext.Listings.SingleOrDefaultAsync(entity => entity.Id == id.Value, cancellationToken);

        if (tracked is null)
        {
            return false;
        }

        _dbContext.Listings.Remove(tracked);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}