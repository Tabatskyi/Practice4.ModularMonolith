namespace WorkflowService.Api;

public sealed record StartBuyListingWorkflowRequest(Guid ListingId, Guid BuyerUserId, bool SimulatePaymentFailure = false);
