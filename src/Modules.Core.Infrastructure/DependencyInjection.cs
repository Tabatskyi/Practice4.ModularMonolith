using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Modules.Core.Application.API.Repos;
using Modules.Core.Infrastructure.Persistence;
using Modules.Core.Infrastructure.Persistence.Mappings;
using Modules.Core.Infrastructure.Persistence.Repositories;

namespace Modules.Core.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddCoreInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString =
            configuration.GetConnectionString("ListingDb")
            ?? configuration.GetConnectionString("CoreDb")
            ?? Environment.GetEnvironmentVariable("ConnectionStrings__CoreDb")
            ?? throw new InvalidOperationException("Connection string for CoreDb was not found in configuration or environment variables.");

        services.AddDbContext<ListingDbContext>(options => options.UseNpgsql(connectionString));
        services.AddAutoMapper(cfg => cfg.AddProfile<ListingMappingProfile>());
        services.AddScoped<IListingRepository, ListingRepository>();

        return services;
    }
}