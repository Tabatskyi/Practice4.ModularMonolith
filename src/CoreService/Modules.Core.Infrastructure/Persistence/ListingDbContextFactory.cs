using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Modules.Core.Infrastructure.Persistence;

public sealed class ListingDbContextFactory : IDesignTimeDbContextFactory<ListingDbContext>
{
    public ListingDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ListingDbContext>();

        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__CoreDb")
            ?? "Host=localhost;Port=5432;Database=practice4_modular_monolith;Username=postgres;Password=postgres";

        optionsBuilder.UseNpgsql(connectionString);

        return new ListingDbContext(optionsBuilder.Options);
    }
}