using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using MediatR;
using Modules.Core.Application.Commands;
using Modules.Core.Infrastructure;
using Modules.Core.Infrastructure.Persistence;

namespace Api;

public class Program
{
    public static void Main(string[] args)
    {

        var builder = WebApplication.CreateBuilder(args);

        builder.Services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(CreateListingCommand).Assembly));
        builder.Services.AddCoreInfrastructure(builder.Configuration);

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
            app.MapGet("/", () => Results.Redirect("/swagger"));
        }

        app.MapPost("/core-items", async (CreateListingRequest request, ISender sender, CancellationToken cancellationToken) =>
        {
            try
            {
                var id = await sender.Send(new CreateListingCommand(request.Title, request.Price),cancellationToken);
                var listing = await sender.Send(new GetListingByIdCommand(id), cancellationToken);

                return listing is null
                    ? Results.Created($"/core-items/{id}", new { id })
                    : Results.Created($"/core-items/{id}", ListingResponse.FromDomain(listing));
            }
            catch (ArgumentException exception)
            {
                return Results.BadRequest(new { error = exception.Message });
            }
            catch (InvalidOperationException exception)
            {
                return Results.BadRequest(new { error = exception.Message });
            }
        });

        app.MapGet("/core-items/{id:guid}", async (Guid id, ISender sender, CancellationToken cancellationToken) =>
        {
            var listing = await sender.Send(new GetListingByIdCommand(id), cancellationToken);
            return listing is null ? Results.NotFound() : Results.Ok(ListingResponse.FromDomain(listing));
        });

        app.MapPatch("/core-items/{id:guid}/status", async (Guid id, UpdateListingStatusRequest request, ISender sender, CancellationToken cancellationToken) =>
        {
            try
            {
                var listing = await sender.Send(
                    new UpdateListingStatusCommand(id, request.NewStatus),
                    cancellationToken);

                return listing is null ? Results.NotFound() : Results.Ok(ListingResponse.FromDomain(listing));
            }
            catch (ArgumentException exception)
            {
                return Results.BadRequest(new { error = exception.Message });
            }
            catch (InvalidOperationException exception)
            {
                return Results.BadRequest(new { error = exception.Message });
            }
        });

        app.MapGet("/health", () => Results.Ok(new { status = "Healthy" }));
        app.Run();
    }

    private static void ApplyMigrationsWithRetry(WebApplication app)
    {
        const int maxAttempts = 10;
        var delay = TimeSpan.FromSeconds(2);
        Exception? lastException = null;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                using var scope = app.Services.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ListingDbContext>();
                dbContext.Database.Migrate();
                app.Logger.LogInformation("Database migrations applied successfully.");
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
                    "Migration attempt {Attempt}/{MaxAttempts} failed. Retrying in {DelaySeconds} seconds...",
                    attempt,
                    maxAttempts,
                    delay.TotalSeconds);

                Thread.Sleep(delay);
            }
        }

        throw new InvalidOperationException("Failed to apply database migrations on startup.", lastException);
    }
}