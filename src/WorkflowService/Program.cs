using Microsoft.EntityFrameworkCore;
using Polly;
using Polly.Extensions.Http;
using Polly.Timeout;
using Shared.Api;
using Shared.Migrations;
using WorkflowService.Api;
using WorkflowService.Clients;
using WorkflowService.Persistence;
using WorkflowService.Workflow;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCorrelationIdMiddleware();

var connectionString =
    builder.Configuration.GetConnectionString("WorkflowDb")
    ?? Environment.GetEnvironmentVariable("ConnectionStrings__WorkflowDb")
    ?? throw new InvalidOperationException("Connection string for WorkflowDb was not found in configuration or environment variables.");

var coreServiceBaseUrl =
    builder.Configuration["CoreService:BaseUrl"]
    ?? Environment.GetEnvironmentVariable("CoreService__BaseUrl")
    ?? throw new InvalidOperationException("Core service base URL was not found. Set CoreService:BaseUrl.");

var usersServiceBaseUrl =
    builder.Configuration["UsersService:BaseUrl"]
    ?? Environment.GetEnvironmentVariable("UsersService__BaseUrl")
    ?? throw new InvalidOperationException("Users service base URL was not found. Set UsersService:BaseUrl.");

builder.Services.AddDbContext<WorkflowDbContext>(options => options.UseNpgsql(connectionString));

var retryPolicy = HttpPolicyExtensions
    .HandleTransientHttpError()
    .Or<TimeoutRejectedException>()
    .WaitAndRetryAsync(
        retryCount: 2,
        sleepDurationProvider: retryAttempt => TimeSpan.FromMilliseconds(200 * retryAttempt));
var timeoutPolicy = Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(5));

builder.Services.AddHttpClient<ICoreServiceClient, CoreServiceClient>(client =>
{
    client.BaseAddress = new Uri(coreServiceBaseUrl, UriKind.Absolute);
    client.Timeout = Timeout.InfiniteTimeSpan;
})
    .AddPolicyHandler(retryPolicy)
    .AddPolicyHandler(timeoutPolicy);

builder.Services.AddHttpClient<IUsersServiceClient, UsersServiceClient>(client =>
{
    client.BaseAddress = new Uri(usersServiceBaseUrl, UriKind.Absolute);
    client.Timeout = Timeout.InfiniteTimeSpan;
})
    .AddPolicyHandler(retryPolicy)
    .AddPolicyHandler(timeoutPolicy);

builder.Services.AddScoped<BuyListingWorkflowRunner>();

var app = builder.Build();

var applyMigrationsOnStartup = builder.Configuration.GetValue("Migrations:ApplyOnStartup", true);
if (applyMigrationsOnStartup)
{
    MigrationRetryHelper.ApplyMigrationsWithRetry<WorkflowDbContext>(
        app.Services,
        app.Logger,
        successMessage: "Workflow database migrations applied successfully.",
        failureMessage: "Failed to apply workflow database migrations on startup.");
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.MapGet("/", () => Results.Redirect("/swagger"));
}

app.UseCorrelationIdMiddleware();

app.MapPost("/workflows/{action}", async (
    string action,
    StartBuyListingWorkflowRequest request,
    BuyListingWorkflowRunner runner,
    CancellationToken cancellationToken) =>
{
    if (!string.Equals(action, WorkflowTypes.BuyListing, StringComparison.OrdinalIgnoreCase))
    {
        return Results.BadRequest(new
        {
            error = $"Unsupported workflow action '{action}'. Supported action: '{WorkflowTypes.BuyListing}'."
        });
    }

    if (request.ListingId == Guid.Empty || request.BuyerUserId == Guid.Empty)
    {
        return Results.BadRequest(new { error = "ListingId and BuyerUserId must be non-empty GUID values." });
    }

    var workflowInstance = await runner.StartAsync(request, cancellationToken);
    var response = WorkflowInstanceResponse.FromEntity(workflowInstance);

    return workflowInstance.State == WorkflowStates.Completed
        ? Results.Created($"/workflows/{workflowInstance.WorkflowId}", response)
        : Results.Conflict(response);
});

app.MapGet("/workflows/{workflowId:guid}", async (
    Guid workflowId,
    WorkflowDbContext dbContext,
    CancellationToken cancellationToken) =>
{
    var workflowInstance = await dbContext.WorkflowInstances
        .AsNoTracking()
        .SingleOrDefaultAsync(x => x.WorkflowId == workflowId, cancellationToken);

    return workflowInstance is null
        ? Results.NotFound()
        : Results.Ok(WorkflowInstanceResponse.FromEntity(workflowInstance));
});

app.MapGet("/health", () => Results.Ok(new { status = "Healthy" }));

app.Run();
