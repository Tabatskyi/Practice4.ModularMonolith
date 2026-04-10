namespace WorkflowService.Workflow;

public static class WorkflowTypes
{
    public const string BuyListing = "buy-listing";
}

public static class WorkflowStates
{
    public const string Started = "Started";
    public const string Reserved = "Reserved";
    public const string Paid = "Paid";
    public const string OwnershipTransferred = "OwnershipTransferred";
    public const string Completed = "Completed";
    public const string Failed = "Failed";
    public const string FailedCompensated = "FailedCompensated";
}
