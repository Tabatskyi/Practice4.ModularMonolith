using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using WorkflowService.Api;
using WorkflowService.Clients;
using WorkflowService.Persistence;
using WorkflowService.Workflow;

namespace WorkflowService.Tests;

public sealed class BuyListingWorkflowRunnerTests
{
    [Fact]
    public async Task StartAsync_WhenPaymentFails_RunsCompensationAndPersistsFailureState()
    {
        await using var dbContext = CreateDbContext();
        var coreClient = new FakeCoreServiceClient();
        var usersClient = new FakeUsersServiceClient();
        var runner = new BuyListingWorkflowRunner(dbContext, coreClient, usersClient, NullLogger<BuyListingWorkflowRunner>.Instance);

        var listingId = Guid.NewGuid();
        var buyerUserId = Guid.NewGuid();

        var instance = await runner.StartAsync(
            new StartBuyListingWorkflowRequest(listingId, buyerUserId, SimulatePaymentFailure: true),
            CancellationToken.None);

        Assert.Equal(WorkflowStates.FailedCompensated, instance.State);
        Assert.NotNull(instance.LastError);
        Assert.Contains("Payment provider rejected", instance.LastError);

        Assert.Equal(2, coreClient.Updates.Count);
        Assert.Equal(CoreListingStatus.Draft, coreClient.Updates[0].Status);
        Assert.Equal(CoreListingStatus.Published, coreClient.Updates[1].Status);

        var persisted = await dbContext.WorkflowInstances
            .AsNoTracking()
            .SingleAsync(x => x.WorkflowId == instance.WorkflowId);

        Assert.Equal(WorkflowStates.FailedCompensated, persisted.State);
    }

    [Fact]
    public async Task StartAsync_WhenAllStepsSucceed_CompletesWorkflow()
    {
        await using var dbContext = CreateDbContext();
        var coreClient = new FakeCoreServiceClient();
        var usersClient = new FakeUsersServiceClient();
        var runner = new BuyListingWorkflowRunner(dbContext, coreClient, usersClient, NullLogger<BuyListingWorkflowRunner>.Instance);

        var instance = await runner.StartAsync(
            new StartBuyListingWorkflowRequest(Guid.NewGuid(), Guid.NewGuid()),
            CancellationToken.None);

        Assert.Equal(WorkflowStates.Completed, instance.State);
        Assert.Null(instance.LastError);

        Assert.Equal(3, coreClient.Updates.Count);
        Assert.Equal(CoreListingStatus.Draft, coreClient.Updates[0].Status);
        Assert.Equal(CoreListingStatus.Published, coreClient.Updates[1].Status);
        Assert.Equal(CoreListingStatus.Sold, coreClient.Updates[2].Status);
    }

    private static WorkflowDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<WorkflowDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new WorkflowDbContext(options);
    }

    private sealed class FakeCoreServiceClient : ICoreServiceClient
    {
        public List<(Guid ListingId, CoreListingStatus Status)> Updates { get; } = [];

        public Task UpdateListingStatusAsync(Guid listingId, CoreListingStatus status, CancellationToken cancellationToken = default)
        {
            Updates.Add((listingId, status));
            return Task.CompletedTask;
        }
    }

    private sealed class FakeUsersServiceClient : IUsersServiceClient
    {
        public Task EnsureUserExistsAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
