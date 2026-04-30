using System.Text.Json.Serialization;
using MassTransit;
using MediatR;
using Modules.Core.Application.API.Users;
using Modules.Core.Application.Commands;
using Modules.Core.Infrastructure;
using Modules.Core.Infrastructure.Persistence;
using Shared.Api;
using Shared.Contracts.Events;
using Shared.Migrations;

namespace Api;

public class Program
{
    private const string CoreItemCreatedEventName = "core-item.created";

    public static void Main(string[] args)
    {

        var builder = WebApplication.CreateBuilder(args);

        var rabbitMqHost = Utils.ResolveRabbitMqSetting(builder.Configuration["RabbitMq:Host"], "RabbitMq__Host", "localhost");
        var rabbitMqPort = Utils.ResolveRabbitMqPort(builder.Configuration["RabbitMq:Port"]);
        var rabbitMqVirtualHost = Utils.ResolveRabbitMqSetting(builder.Configuration["RabbitMq:VirtualHost"], "RabbitMq__VirtualHost", "/");
        var rabbitMqUsername = Utils.ResolveRabbitMqSetting(builder.Configuration["RabbitMq:Username"], "RabbitMq__Username", "guest");
        var rabbitMqPassword = Utils.ResolveRabbitMqSetting(builder.Configuration["RabbitMq:Password"], "RabbitMq__Password", "guest");

        builder.Services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        builder.Services.AddCorrelationIdMiddleware();

        builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(CreateListingCommand).Assembly));
        builder.Services.AddCoreInfrastructure(builder.Configuration);
        builder.Services.AddMassTransit(configurator =>
        {
            configurator.UsingRabbitMq((_, cfg) =>
            {
                cfg.Host(rabbitMqHost, rabbitMqPort, rabbitMqVirtualHost, hostConfigurator =>
                {
                    hostConfigurator.Username(rabbitMqUsername);
                    hostConfigurator.Password(rabbitMqPassword);
                });

                cfg.Message<CoreItemCreatedEvent>(topology =>
                {
                    topology.SetEntityName(CoreItemCreatedEventName);
                });
            });
        });

        var app = builder.Build();

        var applyMigrationsOnStartup = builder.Configuration.GetValue("Migrations:ApplyOnStartup", true);
        if (applyMigrationsOnStartup)
        {
            MigrationRetryHelper.ApplyMigrationsWithRetry<ListingDbContext>(
                app.Services,
                app.Logger,
                successMessage: "Database migrations applied successfully.",
                failureMessage: "Failed to apply database migrations on startup.");
        }

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
            app.MapGet("/", () => Results.Redirect("/swagger"));
        }

        app.UseCorrelationIdMiddleware();

        app.MapPost("/core-items", async (CreateListingRequest request, ISender sender, IPublishEndpoint publishEndpoint, HttpContext httpContext, CancellationToken cancellationToken) =>
        {
            try
            {
                var id = await sender.Send(new CreateListingCommand(request.Title, request.Price, request.OwnerUserId),cancellationToken);

                var correlationId = CorrelationContext.CorrelationId
                    ?? Utils.ResolveCorrelationId(null);
                var coreItemCreatedEvent = new CoreItemCreatedEvent(
                    EventId: Guid.NewGuid(),
                    OccurredAt: DateTimeOffset.UtcNow,
                    CorrelationId: correlationId,
                    CoreItemId: id,
                    OwnerUserId: request.OwnerUserId,
                    Summary: request.Title);

                await publishEndpoint.Publish(coreItemCreatedEvent, publishContext =>
                {
                    publishContext.SetRoutingKey(CoreItemCreatedEventName);
                    publishContext.Headers.Set("correlation_id", correlationId);
                    publishContext.Headers.Set(Utils.CorrelationIdHeaderName, correlationId);

                    if (Guid.TryParse(correlationId, out var parsedCorrelationId))
                    {
                        publishContext.CorrelationId = parsedCorrelationId;
                    }
                }, cancellationToken);

                var listing = await sender.Send(new GetListingByIdCommand(id), cancellationToken);

                return listing is null
                    ? Results.Created($"/core-items/{id}", new { id })
                    : Results.Created($"/core-items/{id}", ListingResponse.FromDomain(listing));
            }
            catch (UserNotFoundException exception)
            {
                return Results.BadRequest(new { error = exception.Message });
            }
            catch (UsersServiceUnavailableException exception)
            {
                return Results.Problem(
                    title: "Users service is unavailable.",
                    detail: exception.Message,
                    statusCode: StatusCodes.Status503ServiceUnavailable);
            }
            catch (UsersServiceTimeoutException exception)
            {
                return Results.Problem(
                    title: "Users service request timed out.",
                    detail: exception.Message,
                    statusCode: StatusCodes.Status504GatewayTimeout);
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
}