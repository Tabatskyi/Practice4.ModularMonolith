using WorkflowService.Persistence;

namespace WorkflowService.Api;

public sealed record WorkflowInstanceResponse(
    Guid WorkflowId,
    string Type,
    string State,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    string? LastError)
{
    public static WorkflowInstanceResponse FromEntity(WorkflowInstanceEntity entity)
    {
        return new WorkflowInstanceResponse(
            entity.WorkflowId,
            entity.Type,
            entity.State,
            entity.CreatedAt,
            entity.UpdatedAt,
            entity.LastError);
    }
}
