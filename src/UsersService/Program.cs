
using Microsoft.EntityFrameworkCore;
using Shared.Api;
using Shared.Migrations;
using UsersService.Contracts;
using UsersService.Persistence;
using UsersService.Persistence.Entities;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCorrelationIdMiddleware();

var connectionString =
	builder.Configuration.GetConnectionString("UsersDb")
	?? Environment.GetEnvironmentVariable("ConnectionStrings__UsersDb")
	?? throw new InvalidOperationException("Connection string for UsersDb was not found in configuration or environment variables.");

builder.Services.AddDbContext<UsersDbContext>(options => options.UseNpgsql(connectionString));

var app = builder.Build();

var applyMigrationsOnStartup = builder.Configuration.GetValue("Migrations:ApplyOnStartup", true);
if (applyMigrationsOnStartup)
{
	MigrationRetryHelper.ApplyMigrationsWithRetry<UsersDbContext>(
		app.Services,
		app.Logger,
		successMessage: "Users database migrations applied successfully.",
		failureMessage: "Failed to apply users database migrations on startup.",
		retryMessageTemplate: "Users migration attempt {Attempt}/{MaxAttempts} failed. Retrying in {DelaySeconds} seconds...");
}

if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
	app.MapGet("/", () => Results.Redirect("/swagger"));
}

app.UseCorrelationIdMiddleware();

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
