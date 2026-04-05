using AutoMapper;
using Modules.Core.Domain;
using Modules.Core.Infrastructure.Persistence.Entities;

namespace Modules.Core.Infrastructure.Persistence.Mappings;

public sealed class ListingMappingProfile : Profile
{
    public ListingMappingProfile()
    {
        CreateMap<Listing, ListingEntity>()
            .ForMember(destination => destination.Id, options => options.MapFrom(source => source.Id.Value))
            .ForMember(destination => destination.OwnerUserId, options => options.MapFrom(source => source.OwnerUserId));

        CreateMap<ListingEntity, Listing>()
            .ConstructUsing(source => new Listing(
                new ListingId(source.Id),
                source.OwnerUserId,
                source.Title,
                source.Price,
                source.Status,
                source.CreatedAtUtc))
            .ForAllMembers(options => options.Ignore());
    }
}