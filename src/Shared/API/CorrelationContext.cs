namespace Shared.Api;

public static class CorrelationContext
{
    private static readonly AsyncLocal<string?> CorrelationIdHolder = new();

    public static string? CorrelationId
    {
        get => CorrelationIdHolder.Value;
        set => CorrelationIdHolder.Value = value;
    }
}