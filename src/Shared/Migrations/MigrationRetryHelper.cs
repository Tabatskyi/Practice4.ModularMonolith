using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Shared.Migrations;

public static class MigrationRetryHelper
{
    public static void ApplyMigrationsWithRetry<TDbContext>(
        IServiceProvider services,
        ILogger logger,
        string successMessage,
        string failureMessage,
        string retryMessageTemplate = "Migration attempt {Attempt}/{MaxAttempts} failed. Retrying in {DelaySeconds} seconds...",
        int maxAttempts = 10,
        TimeSpan? retryDelay = null)
        where TDbContext : DbContext
    {
        var delay = retryDelay ?? TimeSpan.FromSeconds(2);
        Exception? lastException = null;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                using var scope = services.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<TDbContext>();
                dbContext.Database.Migrate();
                logger.LogInformation(successMessage);
                return;
            }
            catch (Exception ex)
            {
                lastException = ex;

                if (attempt == maxAttempts)
                {
                    break;
                }

                logger.LogWarning(
                    ex,
                    retryMessageTemplate,
                    attempt,
                    maxAttempts,
                    delay.TotalSeconds);

                Thread.Sleep(delay);
            }
        }

        throw new InvalidOperationException(failureMessage, lastException);
    }
}