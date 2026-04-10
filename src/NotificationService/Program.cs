using MassTransit;
using Microsoft.EntityFrameworkCore;
using NotificationService.Consumers;
using NotificationService.Persistence;
using Shared.Api;
using Shared.Contracts.Events;
using Shared.Migrations;

const string CoreItemCreatedEventName = "core-item.created";
const string CoreItemCreatedQueueName = "notification.core-item.created";

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCorrelationIdMiddleware();

var connectionString =
	builder.Configuration.GetConnectionString("NotificationDb")
	?? Environment.GetEnvironmentVariable("ConnectionStrings__NotificationDb")
	?? throw new InvalidOperationException("Connection string for NotificationDb was not found in configuration or environment variables.");

var rabbitMqHost = Utils.ResolveRabbitMqSetting(builder.Configuration["RabbitMq:Host"], "RabbitMq__Host", "localhost");
var rabbitMqPort = Utils.ResolveRabbitMqPort(builder.Configuration["RabbitMq:Port"]);
var rabbitMqVirtualHost = Utils.ResolveRabbitMqSetting(builder.Configuration["RabbitMq:VirtualHost"], "RabbitMq__VirtualHost", "/");
var rabbitMqUsername = Utils.ResolveRabbitMqSetting(builder.Configuration["RabbitMq:Username"], "RabbitMq__Username", "guest");
var rabbitMqPassword = Utils.ResolveRabbitMqSetting(builder.Configuration["RabbitMq:Password"], "RabbitMq__Password", "guest");

builder.Services.AddDbContext<NotificationDbContext>(options => options.UseNpgsql(connectionString));

builder.Services.AddMassTransit(configurator =>
{
	configurator.AddConsumer<CoreItemCreatedConsumer>();

	configurator.UsingRabbitMq((context, cfg) =>
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

		cfg.ReceiveEndpoint(CoreItemCreatedQueueName, endpoint =>
		{
			endpoint.ConfigureConsumer<CoreItemCreatedConsumer>(context);
		});
	});
});

var app = builder.Build();

var applyMigrationsOnStartup = builder.Configuration.GetValue("Migrations:ApplyOnStartup", true);
if (applyMigrationsOnStartup)
{
	MigrationRetryHelper.ApplyMigrationsWithRetry<NotificationDbContext>(
		app.Services,
		app.Logger,
		successMessage: "Notification database migrations applied successfully.",
		failureMessage: "Failed to apply notification database migrations on startup.",
		retryMessageTemplate: "Notification migration attempt {Attempt}/{MaxAttempts} failed. Retrying in {DelaySeconds} seconds...");
}

if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
	app.MapGet("/", () => Results.Redirect("/swagger"));
}

app.UseCorrelationIdMiddleware();

app.MapGet("/health", () => Results.Ok(new { status = "Healthy" }));

app.Run();
