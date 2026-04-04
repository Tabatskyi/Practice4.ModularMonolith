using System.Text.Json.Serialization;
using MediatR;
using Modules.Core.Application.Commands;
using Modules.Core.Infrastructure;

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
        builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(CreateListingCommand).Assembly));
        builder.Services.AddCoreInfrastructure(builder.Configuration);

        var app = builder.Build();

        var coreItems = app.MapGroup("/core-items");

        coreItems.MapPost("/", async (CreateListingRequest request, ISender sender, CancellationToken cancellationToken) =>
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

        coreItems.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken cancellationToken) =>
        {
            var listing = await sender.Send(new GetListingByIdCommand(id), cancellationToken);
            return listing is null ? Results.NotFound() : Results.Ok(ListingResponse.FromDomain(listing));
        });

        coreItems.MapPatch("/{id:guid}/status", async (Guid id, UpdateListingStatusRequest request, ISender sender, CancellationToken cancellationToken) =>
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
}