using WorkflowService.Api;
using WorkflowService.Clients;
using WorkflowService.Persistence;

namespace WorkflowService.Workflow;

public sealed class BuyListingWorkflowRunner(
    WorkflowDbContext dbContext,
    ICoreServiceClient coreServiceClient,
    IUsersServiceClient usersServiceClient,
    ILogger<BuyListingWorkflowRunner> logger)
{
    private readonly WorkflowDbContext _dbContext = dbContext;
    private readonly ICoreServiceClient _coreServiceClient = coreServiceClient;
    private readonly IUsersServiceClient _usersServiceClient = usersServiceClient;
    private readonly ILogger<BuyListingWorkflowRunner> _logger = logger;

    public async Task<WorkflowInstanceEntity> StartAsync(
        StartBuyListingWorkflowRequest request,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var workflowInstance = new WorkflowInstanceEntity
        {
            WorkflowId = Guid.NewGuid(),
            Type = WorkflowTypes.BuyListing,
            State = WorkflowStates.Started,
            CreatedAt = now,
            UpdatedAt = now
        };

        _dbContext.WorkflowInstances.Add(workflowInstance);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var listingReserved = false;

        try
        {
            // Reserve by temporarily unpublishing the listing.
            await _coreServiceClient.UpdateListingStatusAsync(request.ListingId, CoreListingStatus.Draft, cancellationToken);
            listingReserved = true;
            await TransitionStateAsync(workflowInstance, WorkflowStates.Reserved, cancellationToken);

            await _usersServiceClient.EnsureUserExistsAsync(request.BuyerUserId, cancellationToken);

            if (request.SimulatePaymentFailure)
            {
                throw new InvalidOperationException("Payment provider rejected the transaction.");
            }

            await TransitionStateAsync(workflowInstance, WorkflowStates.Paid, cancellationToken);

            // Transfer ownership in current domain is represented by moving listing to Sold.
            await _coreServiceClient.UpdateListingStatusAsync(request.ListingId, CoreListingStatus.Published, cancellationToken);
            await _coreServiceClient.UpdateListingStatusAsync(request.ListingId, CoreListingStatus.Sold, cancellationToken);

            await TransitionStateAsync(workflowInstance, WorkflowStates.OwnershipTransferred, cancellationToken);
            await TransitionStateAsync(workflowInstance, WorkflowStates.Completed, cancellationToken);
        }
        catch (Exception exception)
        {
            await HandleFailureAsync(workflowInstance, request.ListingId, listingReserved, exception, cancellationToken);
        }

        return workflowInstance;
    }

    private async Task TransitionStateAsync(
        WorkflowInstanceEntity workflowInstance,
        string state,
        CancellationToken cancellationToken)
    {
        workflowInstance.State = state;
        workflowInstance.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task HandleFailureAsync(
        WorkflowInstanceEntity workflowInstance,
        Guid listingId,
        bool listingReserved,
        Exception exception,
        CancellationToken cancellationToken)
    {
        _logger.LogWarning(
            exception,
            "Workflow {WorkflowId} failed at state {State}.",
            workflowInstance.WorkflowId,
            workflowInstance.State);

        var errorMessage = exception.Message;

        if (listingReserved)
        {
            try
            {
                await _coreServiceClient.UpdateListingStatusAsync(listingId, CoreListingStatus.Published, cancellationToken);
                workflowInstance.State = WorkflowStates.FailedCompensated;
            }
            catch (Exception compensationException)
            {
                _logger.LogError(
                    compensationException,
                    "Workflow {WorkflowId} compensation failed.",
                    workflowInstance.WorkflowId);

                errorMessage = $"{errorMessage} Compensation failed: {compensationException.Message}";
                workflowInstance.State = WorkflowStates.Failed;
            }
        }
        else
        {
            workflowInstance.State = WorkflowStates.Failed;
        }

        workflowInstance.LastError = errorMessage;
        workflowInstance.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
