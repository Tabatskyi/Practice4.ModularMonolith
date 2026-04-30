using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace UsersService.Persistence;

public sealed class UsersDbContextFactory : IDesignTimeDbContextFactory<UsersDbContext>
{
    public UsersDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<UsersDbContext>();

        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__UsersDb")
            ?? "Host=localhost;Port=5433;Database=practice5_users;Username=postgres;Password=postgres";

        optionsBuilder.UseNpgsql(connectionString);

        return new UsersDbContext(optionsBuilder.Options);
    }
}