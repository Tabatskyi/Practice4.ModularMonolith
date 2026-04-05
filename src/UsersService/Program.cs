
using Microsoft.EntityFrameworkCore;
using UsersService.Contracts;
using UsersService.Persistence;
using UsersService.Persistence.Entities;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var connectionString =
	builder.Configuration.GetConnectionString("UsersDb")
	?? Environment.GetEnvironmentVariable("ConnectionStrings__UsersDb")
	?? throw new InvalidOperationException("Connection string for UsersDb was not found in configuration or environment variables.");

builder.Services.AddDbContext<UsersDbContext>(options => options.UseNpgsql(connectionString));

var app = builder.Build();

var applyMigrationsOnStartup = builder.Configuration.GetValue("Migrations:ApplyOnStartup", true);
if (applyMigrationsOnStartup)
{
	ApplyMigrationsWithRetry(app);
}

if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
}

app.MapPost("/users", async (CreateUserRequest request, UsersDbContext dbContext, CancellationToken cancellationToken) =>
{
	if (string.IsNullOrWhiteSpace(request.DisplayName))
	{
		return Results.BadRequest(new { error = "DisplayName is required." });
	}

	var displayName = request.DisplayName.Trim();
	if (displayName.Length > 128)
	{
		return Results.BadRequest(new { error = "DisplayName must be 128 characters or fewer." });
	}

	var user = new UserEntity
	{
		UserId = Guid.NewGuid(),
		DisplayName = displayName
	};

	dbContext.Users.Add(user);
	await dbContext.SaveChangesAsync(cancellationToken);

	return Results.Created($"/users/{user.UserId}", UserResponse.FromEntity(user));
});

app.MapGet("/users/{id:guid}", async (Guid id, UsersDbContext dbContext, CancellationToken cancellationToken) =>
{
	var user = await dbContext.Users
		.AsNoTracking()
		.SingleOrDefaultAsync(x => x.UserId == id, cancellationToken);

	return user is null ? Results.NotFound() : Results.Ok(UserResponse.FromEntity(user));
});

app.MapGet("/health", () => Results.Ok(new { status = "Healthy" }));

app.Run();

static void ApplyMigrationsWithRetry(WebApplication app)
{
	const int maxAttempts = 10;
	var delay = TimeSpan.FromSeconds(2);
	Exception? lastException = null;

	for (var attempt = 1; attempt <= maxAttempts; attempt++)
	{
		try
		{
			using var scope = app.Services.CreateScope();
			var dbContext = scope.ServiceProvider.GetRequiredService<UsersDbContext>();
			dbContext.Database.Migrate();
			app.Logger.LogInformation("Users database migrations applied successfully.");
			return;
		}
		catch (Exception ex)
		{
			lastException = ex;

			if (attempt == maxAttempts)
			{
				break;
			}

			app.Logger.LogWarning(
				ex,
				"Users migration attempt {Attempt}/{MaxAttempts} failed. Retrying in {DelaySeconds} seconds...",
				attempt,
				maxAttempts,
				delay.TotalSeconds);

			Thread.Sleep(delay);
		}
	}

	throw new InvalidOperationException("Failed to apply users database migrations on startup.", lastException);
}
