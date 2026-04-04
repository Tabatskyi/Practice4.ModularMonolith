using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Modules.Core.Infrastructure.Persistence;

public sealed class ListingDbContextFactory : IDesignTimeDbContextFactory<ListingDbContext>
{
    public ListingDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ListingDbContext>();

        var connectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__ListingDb")
            ?? throw new InvalidOperationException("Connection string for ListingDb was not found in environment variables.");

        optionsBuilder.UseNpgsql(connectionString);

        return new ListingDbContext(optionsBuilder.Options);
    }
}