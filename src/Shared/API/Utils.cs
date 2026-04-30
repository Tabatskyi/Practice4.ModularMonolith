namespace Shared.Api;

public static class Utils
{
    public const string CorrelationIdHeaderName = "X-Correlation-Id";

    public static string ResolveCorrelationId(string? correlationIdHeaderValue)
    {
        if (!string.IsNullOrWhiteSpace(correlationIdHeaderValue))
        {
            return correlationIdHeaderValue;
        }

        return Guid.NewGuid().ToString();
    }

    public static string ResolveRabbitMqSetting(string? configuredValue, string envKey, string fallback)
    {
        return configuredValue
            ?? Environment.GetEnvironmentVariable(envKey)
            ?? fallback;
    }

    public static ushort ResolveRabbitMqPort(string? configuredPortValue, string envKey = "RabbitMq__Port", ushort fallback = 5672)
    {
        if (ushort.TryParse(configuredPortValue, out var parsedConfiguredPort))
        {
            return parsedConfiguredPort;
        }

        var environmentPort = Environment.GetEnvironmentVariable(envKey);
        return ushort.TryParse(environmentPort, out var parsedPort) ? parsedPort : fallback;
    }
}