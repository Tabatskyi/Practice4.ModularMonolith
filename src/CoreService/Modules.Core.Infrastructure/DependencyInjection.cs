using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Modules.Core.Application.API.Repos;
using Modules.Core.Application.API.Users;
using Modules.Core.Infrastructure.Persistence;
using Modules.Core.Infrastructure.Persistence.Mappings;
using Modules.Core.Infrastructure.Persistence.Repositories;
using Modules.Core.Infrastructure.Users;
using Polly;
using Polly.Extensions.Http;
using Polly.Timeout;

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

        var usersServiceBaseUrl =
            configuration["UsersService:BaseUrl"]
            ?? Environment.GetEnvironmentVariable("UsersService__BaseUrl")
            ?? throw new InvalidOperationException("Users service base URL was not found. Set UsersService:BaseUrl.");

        services.AddDbContext<ListingDbContext>(options => options.UseNpgsql(connectionString));
        services.AddAutoMapper(cfg => cfg.AddProfile<ListingMappingProfile>());
        services.AddScoped<IListingRepository, ListingRepository>();
        var retryPolicy = HttpPolicyExtensions
            .HandleTransientHttpError()
            .Or<TimeoutRejectedException>()
            .WaitAndRetryAsync(
                retryCount: 2,
                sleepDurationProvider: retryAttempt => TimeSpan.FromMilliseconds(200 * retryAttempt));
        var timeoutPolicy = Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(3));

        services.AddHttpClient<IUsersServiceClient, UsersServiceClient>(client =>
        {
            client.BaseAddress = new Uri(usersServiceBaseUrl, UriKind.Absolute);
            client.Timeout = Timeout.InfiniteTimeSpan;
        })
            .AddPolicyHandler(retryPolicy)
            .AddPolicyHandler(timeoutPolicy);

        return services;
    }
}