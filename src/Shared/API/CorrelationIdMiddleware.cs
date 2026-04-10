using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Shared.Api;

public sealed class CorrelationIdMiddleware(ILogger<CorrelationIdMiddleware> logger) : IMiddleware
{
    private readonly ILogger<CorrelationIdMiddleware> _logger = logger;

    public async Task InvokeAsync(HttpContext context, RequestDelegate next) 
    {
        var correlationIdHeaderValue = context.Request.Headers.TryGetValue(Utils.CorrelationIdHeaderName, out var headerValue)
            ? headerValue.ToString()
            : null;
        var correlationId = Utils.ResolveCorrelationId(correlationIdHeaderValue);

        context.Request.Headers[Utils.CorrelationIdHeaderName] = correlationId;
        context.Response.Headers[Utils.CorrelationIdHeaderName] = correlationId;

        var previousCorrelationId = CorrelationContext.CorrelationId;
        CorrelationContext.CorrelationId = correlationId;

        try
        {
            using (_logger.BeginScope("CorrelationId:{CorrelationId}", correlationId))
            {
                await next(context);
            }
        }
        finally
        {
            CorrelationContext.CorrelationId = previousCorrelationId;
        }
    }
}

public static class CorrelationIdMiddlewareExtensions
{
    public static IServiceCollection AddCorrelationIdMiddleware(this IServiceCollection services)
    {
        services.AddTransient<CorrelationIdMiddleware>();
        return services;
    }

    public static IApplicationBuilder UseCorrelationIdMiddleware(this IApplicationBuilder app)
    {
        return app.UseMiddleware<CorrelationIdMiddleware>();
    }
}