namespace WorkflowService.Persistence;

public sealed class WorkflowInstanceEntity
{
    public Guid WorkflowId { get; set; }

    public string Type { get; set; } = string.Empty;

    public string State { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public string? LastError { get; set; }
}
